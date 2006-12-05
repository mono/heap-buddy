/* -*- Mode: C; tab-width: 8; indent-tabs-mode: nil; c-basic-offset: 8 -*- */

/*
 * accountant.c
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
#include "accountant.h"

#include <mono/metadata/mono-gc.h>
#include <mono/metadata/debug-helpers.h>

typedef struct _LiveObject LiveObject;
struct _LiveObject {
        MonoObject *obj;
        int size;
        int age;
};

Accountant *
accountant_new (MonoClass *klass, StackFrame **backtrace)
{
        Accountant *acct = g_new0 (Accountant, 1);
        acct->klass = klass;
        acct->backtrace = backtrace;

        return acct;
}

void
accountant_register_object (Accountant *acct, MonoObject *obj, int obj_size)
{
        LiveObject *live = g_new0 (LiveObject, 1);
        GSList *iter;

        live->obj    = obj;
        live->size   = obj_size;
        live->age    = 0;

        // Clean up any dead objects
        if (acct->dead_objects != NULL) {
                for (iter = acct->dead_objects; iter != NULL; iter = iter->next)
                        g_free (iter->data);
                g_slist_free (acct->dead_objects);
                acct->dead_objects = NULL;
        }

        acct->live_objects = g_list_prepend (acct->live_objects, live);
        
        acct->n_allocated_objects++;
        acct->n_allocated_bytes += live->size;

        acct->n_live_objects++;
        acct->n_live_bytes += live->size;

        acct->dirty = TRUE;
}

void
accountant_post_gc_processing (Accountant *acct)
{
        GList *iter;

        iter = acct->live_objects;
        if (iter != NULL)
                acct->dirty = TRUE;
        while (iter != NULL) {
                LiveObject *live = iter->data;
                GList *next_iter = iter->next;
                if (mono_object_is_alive (live->obj)) {
                        acct->allocated_total_age++;
                        live->age++;
                        acct->live_total_age++;

                        acct->allocated_total_weight += live->size;
                        acct->live_total_weight += live->size;
                } else {
                        acct->n_live_objects--;
                        acct->n_live_bytes -= live->size;
                        acct->live_total_age -= live->age;
                        acct->live_total_weight -= live->size * live->age;

                        acct->live_objects = g_list_delete_link (acct->live_objects, iter);

                        // Calling g_free at this point can cause deadlocks, so we put the
                        // dead objects into a list to be g_freed later.
                        acct->dead_objects = g_slist_prepend (acct->dead_objects, live);
                }
                iter = next_iter;
        }
}
