/* -*- Mode: C; tab-width: 8; indent-tabs-mode: nil; c-basic-offset: 8 -*- */

/*
 * backtrace.c
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

#include <mono/metadata/mono-debug.h>
#include "backtrace.h"

struct HashAndCountInfo {
        gint32 hash;
        int count;
};

static gboolean
stack_walk_hash_and_count_fn (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data)
{
        struct HashAndCountInfo *info = data;
        
        const guint32 full_c   = 2909;
        const guint32 method_c = 277;
        const guint32 native_c = 163;
        const guint32 il_c     = 47;

        guint32 method_hash = GPOINTER_TO_UINT (method);
        
        info->hash = full_c * info->hash
                + method_c * method_hash
                + native_c * native_offset
                + il_c * il_offset
                + (managed ? 1 : 0);
        info->count++;
        
        return FALSE;
}

struct FrameInfo {
        int pos;
        StackFrame **vec;
};

static gboolean
stack_walk_build_frame_vector_fn (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data)
{
        struct FrameInfo *info = data;

        StackFrame *frame;
        frame = g_new0 (StackFrame, 1);
        
        frame->method        = method;
        frame->native_offset = native_offset;
        frame->il_offset     = il_offset;
        frame->managed       = managed;

        info->vec [info->pos] = frame;
        ++info->pos;

        return FALSE;
}

StackFrame **
backtrace_get_current ()
{
        static GHashTable *backtrace_cache = NULL;

        struct HashAndCountInfo hc_info;
        struct FrameInfo frame_info;

        if (backtrace_cache == NULL)
                backtrace_cache = g_hash_table_new (NULL, NULL);

        hc_info.hash = 0;
        hc_info.count = 0;
        mono_stack_walk_no_il (stack_walk_hash_and_count_fn, &hc_info);

        StackFrame **frame_vec;
        frame_vec = g_hash_table_lookup (backtrace_cache, GUINT_TO_POINTER (hc_info.hash));
        if (frame_vec != NULL)
                return frame_vec;

        // FIXME: we need to deal with hash collisions

        frame_vec = g_new0 (StackFrame *, hc_info.count + 1);
        frame_info.pos = 0;
        frame_info.vec = frame_vec;
        mono_stack_walk_no_il (stack_walk_build_frame_vector_fn, &frame_info);
        g_hash_table_insert (backtrace_cache, GUINT_TO_POINTER (hc_info.hash), frame_vec);

        return frame_vec;
}
