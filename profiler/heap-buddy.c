/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */

/*
 * heap-buddy.c
 *
 * Copyright (C) 2005 Novell, Inc.
 *
 */

/*
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of version 2 of the GNU General Public
 * License as published by the Free Software Foundation.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307
 * USA.
 */

#include <assert.h>
#include <stdlib.h>
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

GHashTable *memlog = NULL;
const char *memlog_name = "outfile.types";
FILE *memlog_file = NULL;

void
init_memory_logging ( )
{
	memlog_file = fopen (memlog_name, "w+");
	
	memlog = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, g_free);
}

void
clear_memory_log ( )
{
	g_hash_table_destroy (memlog);
	memlog = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, g_free);
}

void
close_memory_logging ( )
{
	g_hash_table_destroy (memlog);

	fclose (memlog_file);
}

void
memlog_add_alloc (MonoObject *obj, MonoClass *klass)
{
	const char *name;
	gint32 *size;
	
	size = g_new0 (gint32, 1);
		
	*size = mono_object_get_size (obj);
	name  = mono_class_get_name (klass);
	
	//type = g_hash_table_lookup (memlog, name);
	
	gpointer old_key;
	gpointer old_val;
	
	if (g_hash_table_lookup_extended (memlog, name, &old_key, &old_val))
		*(gint32 *)old_val += *size;
	else
		g_hash_table_insert (memlog, g_strdup (name), GINT_TO_POINTER(size));

}

void
memlog_add_gc_obj (Accountant *acct)
{
	const char * name = mono_class_get_name (acct->klass);
	
	gpointer old_key;
	gpointer old_val;
	
	if (g_hash_table_lookup_extended (memlog, name, &old_key, &old_val)) {
		*(gint32 *)old_val = acct->n_allocated_bytes;
	}
}

void
memlog_update_log_file (gpointer key, gpointer val, gpointer data)
{
	type_writer_update_types (memlog_file, (const char *)key, *(gint32 *)val);
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
	
	memlog_add_alloc (obj, klass);

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
	
	memlog_add_gc_obj (acct);

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
	
	type_writer_start_types (memlog_file, memlog);

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
			      
	g_hash_table_foreach (memlog, memlog_update_log_file, NULL);
	clear_memory_log ();

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
	
	close_memory_logging ();
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
	
	init_memory_logging ();
	
	mono_profiler_install_allocation (heap_buddy_alloc_func);

	mono_profiler_install_gc (heap_buddy_gc_func, heap_buddy_gc_resize_func);

	mono_profiler_set_events (MONO_PROFILE_ALLOCATIONS | MONO_PROFILE_GC);
	
	p = create_mono_profiler (outfilename);

	mono_profiler_install (p, heap_buddy_shutdown);
}
