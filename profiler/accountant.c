/* -*- Mode: C; tab-width: 8; indent-tabs-mode: nil; c-basic-offset: 8 -*- */

/*
 * accountant.c
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
