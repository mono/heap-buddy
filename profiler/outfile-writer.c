/* -*- Mode: C; tab-width: 8; indent-tabs-mode: nil; c-basic-offset: 8 -*- */

/*
 * outfile-writer.c
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
#include <time.h>

#include "outfile-writer.h"

#define MAGIC_NUMBER 0x4eabbdd1
#define FILE_FORMAT_VERSION 5
#define FILE_LABEL "heap-buddy logfile"

#define TAG_TYPE    0x01
#define TAG_METHOD  0x02
#define TAG_CONTEXT 0x03
#define TAG_GC      0x04
#define TAG_RESIZE  0x05
#define TAG_EOS     0xff

static void
write_byte (FILE *out, guint8 x)
{
        fwrite (&x, sizeof (guint8), 1, out);
}

static void
write_pointer (FILE *out, gpointer x)
{
        guint32 y = GPOINTER_TO_UINT (x);
        fwrite (&y, sizeof (guint32), 1, out);
}

static void
write_int16 (FILE *out, gint16 x)
{
        fwrite (&x, sizeof (gint16), 1, out);
}

static void
write_uint16 (FILE *out, guint16 x)
{
        fwrite (&x, sizeof (guint16), 1, out);
}

static void
write_int32 (FILE *out, gint32 x)
{
        fwrite (&x, sizeof (gint32), 1, out);
}

static void
write_uint32 (FILE *out, guint32 x)
{
        fwrite (&x, sizeof (guint32), 1, out);
}

static void
write_int64 (FILE *out, gint64 x)
{
        fwrite (&x, sizeof (gint64), 1, out);
}


static void
write_vint (FILE *out, guint32 x)
{
        guint8 y;

        do {
                y = (guint8) (x & 0x7f);
                x = x >> 7;
                if (x != 0)
                        y |= 0x80;
                write_byte (out, y);
        } while (x != 0);
}

static void
write_string (FILE *out, const char *str)
{
        int len = strlen (str);
        write_vint (out, (guint32) len);
        fwrite (str, sizeof (char), len, out);
}

OutfileWriter *
outfile_writer_open (const char *filename)
{
        OutfileWriter *ofw;
        ofw = g_new0 (OutfileWriter, 1);
        ofw->out = fopen (filename, "w");
        ofw->seen_items = g_hash_table_new (NULL, NULL);
        ofw->gc_count = 0;

        write_uint32 (ofw->out, MAGIC_NUMBER);
        write_int32  (ofw->out, FILE_FORMAT_VERSION);
        write_string (ofw->out, FILE_LABEL);

        ofw->saved_outfile_offset = ftell (ofw->out);

        // we update these after every GC
        write_byte (ofw->out, 0);   // is the log fully written out? 
        write_int32 (ofw->out, -1); // total # of GCs
        write_int32 (ofw->out, -1); // total # of types
        write_int32 (ofw->out, -1); // total # of methods
        write_int32 (ofw->out, -1); // total # of contexts/backtraces
        write_int32 (ofw->out, -1); // total # of resizes
        write_int64 (ofw->out, -1); // total # of allocated bytes
        write_int32 (ofw->out, -1); // total # of allocated objects

        return ofw;
}

static void
outfile_writer_update_totals (OutfileWriter *ofw,
                              gint64         total_allocated_bytes,
                              gint32         total_allocated_objects)
{
        // Seek back up to the right place in the header
        fseek (ofw->out, ofw->saved_outfile_offset, SEEK_SET);

        // Update our counts and totals
        write_byte (ofw->out, 0); // we still might terminate abnormally
        write_int32 (ofw->out, ofw->gc_count);
        write_int32 (ofw->out, ofw->type_count);
        write_int32 (ofw->out, ofw->method_count);
        write_int32 (ofw->out, ofw->context_count);
        write_int32 (ofw->out, ofw->resize_count);

        if (total_allocated_bytes >= 0) {
                write_int64 (ofw->out, total_allocated_bytes);
                write_int32 (ofw->out, total_allocated_objects);
        }

        // Seek back to the end of the outfile
        fseek (ofw->out, 0, SEEK_END);
}

void
outfile_writer_close (OutfileWriter *ofw)
{
        // Write out the end-of-stream tag.
        write_byte (ofw->out, TAG_EOS);

        // Seek back up to the right place in the header
        fseek (ofw->out, ofw->saved_outfile_offset, SEEK_SET);

        // Indicate that we terminated normally.
        write_byte (ofw->out, 1);

        fclose (ofw->out);
}

void
outfile_writer_add_accountant (OutfileWriter *ofw,
                               Accountant    *acct)
{
        int i, frame_count;
        char *name;

        /* First, add this type if we haven't seen it before. */
        if (g_hash_table_lookup (ofw->seen_items, acct->klass) == NULL) {
                name = mono_type_full_name (mono_class_get_type (acct->klass));
                write_byte (ofw->out, TAG_TYPE);
                write_pointer (ofw->out, acct->klass);
                write_string (ofw->out, name);
                g_free (name);
                g_hash_table_insert (ofw->seen_items, acct->klass, acct->klass);
                ++ofw->type_count;
        }

        /* Next, walk across the backtrace and add any previously-unseen
           methods. */
        frame_count = 0;
        for (i = 0; acct->backtrace [i] != NULL; ++i) {
                ++frame_count;
                MonoMethod *method = acct->backtrace [i]->method;
                if (g_hash_table_lookup (ofw->seen_items, method) == NULL) {
                        char *name = mono_method_full_name (method, TRUE);
                        write_byte (ofw->out, TAG_METHOD);
                        write_pointer (ofw->out, method);
                        write_string (ofw->out, name);
                        g_free (name);
                        g_hash_table_insert (ofw->seen_items, method, method);
                        ++ofw->method_count;
                }
        }

        /* Now we can spew out the accountant's context */
        write_byte (ofw->out, TAG_CONTEXT);
        write_pointer (ofw->out, acct);
        write_pointer (ofw->out, acct->klass);
        write_int16 (ofw->out, frame_count);
        for (i = 0; acct->backtrace [i] != NULL; ++i) {
                write_pointer (ofw->out, acct->backtrace [i]->method);
                write_uint32 (ofw->out, acct->backtrace [i]->il_offset);
        }
        ++ofw->context_count;

        fflush (ofw->out);
}

// total_live_bytes is the total size of all of the live objects
// before the GC
void
outfile_writer_gc_begin (OutfileWriter *ofw,
                         gboolean       is_final, 
                         gint64         total_live_bytes, 
                         gint32         total_live_objects,
                         gint32         n_accountants)
{
        time_t timestamp;
        time (&timestamp);

        write_byte (ofw->out, TAG_GC);
        write_int32 (ofw->out, is_final ? -1 : ofw->gc_count);
        write_int64 (ofw->out, (gint64) timestamp);
        write_int64 (ofw->out, total_live_bytes);
        write_int32 (ofw->out, total_live_objects);
        write_int32 (ofw->out, n_accountants);

        ++ofw->gc_count;
}

void
outfile_writer_gc_log_stats (OutfileWriter *ofw,
                             Accountant    *acct)
{
        write_pointer (ofw->out, acct);
        write_uint32 (ofw->out, acct->n_allocated_objects);
        write_uint32 (ofw->out, acct->n_allocated_bytes);
        write_uint32 (ofw->out, acct->allocated_total_age);
        write_uint32 (ofw->out, acct->allocated_total_weight);
        write_uint32 (ofw->out, acct->n_live_objects);
        write_uint32 (ofw->out, acct->n_live_bytes);
        write_uint32 (ofw->out, acct->live_total_age);
        write_uint32 (ofw->out, acct->live_total_weight);
}

// total_live_bytes is the total size of all live objects
// after the GC is finished
void
outfile_writer_gc_end (OutfileWriter *ofw,
                       gint64         total_allocated_bytes,
                       gint32         total_allocated_objects,
                       gint64         total_live_bytes,
                       gint32         total_live_objects)
{
        write_int64 (ofw->out, total_live_bytes);
        write_int32 (ofw->out, total_live_objects);
        outfile_writer_update_totals (ofw, total_allocated_bytes, total_allocated_objects);
        fflush (ofw->out);
}

void
outfile_writer_resize (OutfileWriter *ofw,
                       gint64         new_size,
                       gint64         total_live_bytes)
{
        time_t timestamp;
        time (&timestamp);

        write_byte (ofw->out, TAG_RESIZE);
        write_int64 (ofw->out, (gint64) timestamp);
        write_int64 (ofw->out, new_size);
        write_int64 (ofw->out, total_live_bytes);
        ++ofw->resize_count;
        outfile_writer_update_totals (ofw, -1, -1);
        fflush (ofw->out);
}

