/* -*- Mode: C; tab-width: 8; indent-tabs-mode: nil; c-basic-offset: 8 -*- */

/*
 * accountant.h
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

