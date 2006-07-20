/* -*- Mode: C; tab-width: 8; indent-tabs-mode: nil; c-basic-offset: 8 -*- */

/*
 * backtrace.h
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

#ifndef __BACKTRACE_H__
#define __BACKTRACE_H__

#include <glib.h>
#include <mono/metadata/debug-helpers.h>

typedef struct _StackFrame StackFrame;
struct _StackFrame {
        MonoMethod *method;
        gint32      native_offset;
        gint32      il_offset;
        gboolean    managed;
};

/* Returns a NULL-terminated vector of StackFrames */
StackFrame **backtrace_get_current ();

#endif /* __BACKTRACE_H__ */

