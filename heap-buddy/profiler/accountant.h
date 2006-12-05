/* -*- Mode: C; tab-width: 8; indent-tabs-mode: nil; c-basic-offset: 8 -*- */

/*
 * accountant.h
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
#ifndef __ACCOUNTANT_H__
#define __ACCOUNTANT_H__

#include <glib.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>

#include "backtrace.h"

typedef struct _Accountant Accountant;

struct _Accountant {

        MonoClass   *klass;
        StackFrame **backtrace;

        guint32   n_allocated_objects;
        guint32   n_allocated_bytes;
        guint32   allocated_total_age;
        guint32   allocated_total_weight;
        guint32   n_live_objects;
        guint32   n_live_bytes;
        guint32   live_total_age;
        guint32   live_total_weight;

        GList  *live_objects;
        GSList *dead_objects;

        gboolean dirty;
};

Accountant *accountant_new                (MonoClass *klass, StackFrame **backtrace);

void        accountant_register_object    (Accountant *acct, MonoObject *obj, int obj_size);

void        accountant_post_gc_processing (Accountant *acct);

#endif /* __ACCOUNTANT_H__ */

