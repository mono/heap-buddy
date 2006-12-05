//
// Report.cs
//
// Copyright (C) 2005 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
