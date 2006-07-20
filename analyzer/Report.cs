//
// Report.cs
//
// Copyright (C) 2005 Novell, Inc.
//

//
// This program is free software; you can redistribute it and/or
// modify it under the terms of version 2 of the GNU General Public
// License as published by the Free Software Foundation.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307
// USA.
//

using System;
using System.Collections;
using System.Reflection;

namespace HeapBuddy {
	
	abstract public class Report {

		private string name;

		protected Report (string name)
		{
			this.name = name;
		}

		public string Name {
			get { return name; }
		}

		abstract public void Run (OutfileReader reader, string [] command_line_args);

		///////////////////////////////////////////////////////////

		static Hashtable by_name = new Hashtable ();

		static Report ()
		{
			Assembly assembly;
			assembly = Assembly.GetExecutingAssembly ();

			foreach (System.Type t in assembly.GetTypes ()) {
				if (t.IsSubclassOf (typeof (Report)) && ! t.IsAbstract) {
					Report report;
					report = (Report) Activator.CreateInstance (t);
					by_name [report.Name.ToLower ()] = report;
				}
			}
		}

		static public Report Get (string name)
		{
			return (Report) by_name [name.ToLower ()];
		}

		static public bool Exists (string name)
		{
			return by_name.Contains (name.ToLower ());
		}
	}
}
