/* -*- Mode: C; tab-width: 8; indent-tabs-mode: nil; c-basic-offset: 8 -*- */

/*
 * outfile-writer.h
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

#ifndef __OUTFILE_WRITER_H__
#define __OUTFILE_WRITER_H__

#include <glib.h>
#include "accountant.h"

typedef struct _OutfileWriter OutfileWriter;
struct _OutfileWriter {
        FILE *out;
        GHashTable *seen_items;
        int gc_count;
        int type_count;
        int method_count;
        int context_count;
        int resize_count;
        long saved_outfile_offset;
};

OutfileWriter *outfile_writer_open           (const char *filename);

void           outfile_writer_close          (OutfileWriter *ofw);

void           outfile_writer_add_accountant (OutfileWriter *ofw, Accountant *acct);

void           outfile_writer_gc_begin       (OutfileWriter *ofw, 
                                              gboolean       is_final, 
                                              gint64         total_live_bytes, 
                                              gint32         total_live_objects,
                                              gint32         n_accountants);

void           outfile_writer_gc_log_stats   (OutfileWriter *ofw, Accountant *acct);

void           outfile_writer_gc_end         (OutfileWriter *ofw,
                                              gint64         total_allocated_bytes,
                                              gint32         total_allocated_objects,
                                              gint64         total_live_bytes,
                                              gint32         total_live_objects);

void           outfile_writer_resize         (OutfileWriter *ofw, gint64 new_size, gint64 total_live_bytes);

#endif /* __OUTFILE_WRITER_H__ */

