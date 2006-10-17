/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */

/*
 * heap-buddy.c
 *
 * Copyright (C) 2005 Novell, Inc.
 *
 */
/*
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#include <string.h>
#include <glib.h>
#include <mono/metadata/assembly.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/metadata/class.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/mono-gc.h>
#include <unistd.h>

#include "accountant.h"
#include "backtrace.h"
#include "outfile-writer.h"


struct _MonoProfiler {
	mono_mutex_t   lock;
	GHashTable    *accountant_hash;
	gint64         total_allocated_bytes;
	gint64         total_live_bytes;
	gint32         total_allocated_objects;
	gint32         total_live_objects;
	gint32         n_dirty_accountants;
	OutfileWriter *outfile_writer;
};

static MonoProfiler *
create_mono_profiler (const char *outfilename)
{
	MonoProfiler *p = g_new0 (MonoProfiler, 1);

	mono_mutex_init (&p->lock, NULL);

	p->accountant_hash  = g_hash_table_new (NULL, NULL);
	p->total_live_bytes = 0;
	p->outfile_writer   = outfile_writer_open (outfilename);

	return p;
}

/* ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** */

static void
heap_buddy_alloc_func (MonoProfiler *p, MonoObject *obj, MonoClass *klass)
{
	Accountant *acct;
	StackFrame **backtrace;
	int size;

	backtrace = backtrace_get_current ();

	mono_mutex_lock (&p->lock);

	acct = g_hash_table_lookup (p->accountant_hash, backtrace);
	if (acct == NULL) {
		acct = accountant_new (klass,  backtrace);
		g_hash_table_insert (p->accountant_hash, backtrace, acct);
		outfile_writer_add_accountant (p->outfile_writer, acct);
	}

	size = mono_object_get_size (obj);
	accountant_register_object (acct, obj, size);
	p->total_allocated_bytes += size;
	p->total_live_bytes += size;
	p->total_allocated_objects++;
	p->total_live_objects++;

	mono_mutex_unlock (&p->lock);
}

static void
post_gc_tallying_fn (gpointer key, gpointer value, gpointer user_data)
{
	MonoProfiler *p = user_data;
	Accountant *acct = value;

	accountant_post_gc_processing (acct);
	p->total_live_bytes += acct->n_live_bytes;
	p->total_live_objects += acct->n_live_objects;
	if (acct->dirty)
		++p->n_dirty_accountants;	
}

static void
post_gc_logging_fn (gpointer key, gpointer value, gpointer user_data)
{
	MonoProfiler *p = user_data;
	Accountant *acct = value;

	// Only log the accountant's stats to the outfile if
	// something has changed since the last time it was logged.
	if (acct->dirty) {
		outfile_writer_gc_log_stats (p->outfile_writer, acct);
		acct->dirty = FALSE;
	}
}

static void
heap_buddy_gc_func (MonoProfiler *p, MonoGCEvent e, int gen)
{
	gint64 prev_total_live_bytes;
	gint32 prev_total_live_objects;
	
	if (e != MONO_GC_EVENT_MARK_END)
		return;

	mono_mutex_lock (&p->lock);

	prev_total_live_bytes = p->total_live_bytes;
	prev_total_live_objects = p->total_live_objects;

	p->total_live_bytes = 0;
	p->total_live_objects = 0;
	p->n_dirty_accountants = 0;
	g_hash_table_foreach (p->accountant_hash, post_gc_tallying_fn, p);
	
	outfile_writer_gc_begin (p->outfile_writer,
				 gen < 0, // negative gen == this is final
				 prev_total_live_bytes,
				 prev_total_live_objects,
				 p->n_dirty_accountants);
	g_hash_table_foreach (p->accountant_hash, post_gc_logging_fn, p);
	outfile_writer_gc_end (p->outfile_writer,
			       p->total_allocated_bytes,
			       p->total_allocated_objects,
			       p->total_live_bytes,
			       p->total_live_objects);

	mono_mutex_unlock (&p->lock);
}

static void
heap_buddy_gc_resize_func (MonoProfiler *p, gint64 new_size)
{
	mono_mutex_lock (&p->lock);

	outfile_writer_resize (p->outfile_writer, new_size, p->total_live_bytes);

	mono_mutex_unlock (&p->lock);
}

static void
heap_buddy_shutdown (MonoProfiler *p)
{
	// Do a final, synthetic GC
	heap_buddy_gc_func (p, MONO_GC_EVENT_MARK_END, -1);

	outfile_writer_close (p->outfile_writer);
}

void
mono_profiler_startup (const char *desc)
{
	MonoProfiler *p;

	const char *outfilename;

	g_assert (! strncmp (desc, "heap-buddy", 10));

	outfilename = strchr (desc, ':');
	if (outfilename == NULL)
		outfilename = "outfile";
	else {
		// Advance past the : and use the rest as the name.
		++outfilename;
	}

	g_print ("*** Running with heap-buddy ***\n");
	
	mono_profiler_install_allocation (heap_buddy_alloc_func);

	mono_profiler_install_gc (heap_buddy_gc_func, heap_buddy_gc_resize_func);

	mono_profiler_set_events (MONO_PROFILE_ALLOCATIONS | MONO_PROFILE_GC);
	
	p = create_mono_profiler (outfilename);

	mono_profiler_install (p, heap_buddy_shutdown);
}
