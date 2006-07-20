//
// MemlogReport.cs
// based on BacktracesReport.cs
//

//
// BacktracesReport.cs
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
using System.IO;
using System.Text;

namespace HeapBuddy {

	public class MemlogReport : Report {
		Hashtable types;
		Array typeArray;	
		int max_rows = 10;
		ulong prog_bytes = 0;
		TypeZone CurrentZone;
		
		SortedList CurrentList = null;
		string CurrentPath = "/";
		const string PS1 = ">> ";
		
		enum Operand {
			OP_NONE,
			OP_HELP,
			OP_LIST,
			OP_SHOW,
			OP_SELECT
		}
		
		public MemlogReport () : base ("Memlog") { }

		public class TypeZone {
			public uint count;
			public uint bytes;
			public string name;
			public Hashtable callers;
			
			public TypeZone ()
			{
				callers = new Hashtable ();
				count = 0;
				bytes = 0;
			}
			
			public TypeZone (string n)
			{
				name = n;
				callers = new Hashtable ();
				count = 0;
				bytes = 0;
			}
		}

		/*
		 * Make sure that all the methods in the
		 * call trace exist in the backtrace data
		 * structure.  Add it if it doesn't, do
		 * nothing if it already exists.
		 */
		public void MergeBacktrace (Backtrace bt, TypeZone tz)
		{
			Hashtable callers = tz.callers;
		
			foreach (Frame f in bt.Frames) {
				if (f.MethodName.StartsWith ("(wrapper"))
					continue;
			
				if (!callers.ContainsKey (f.MethodName))
					callers.Add (f.MethodName, new TypeZone (f.MethodName));
								
				callers = ((TypeZone)callers[f.MethodName]).callers;
			}
		}
		
		/*
		 * Add in the number of allocations and the number
		 * of bytes in each method of this calltrace in the
		 * backtrace data structure.
		 */
		public void UpdateBacktrace (Backtrace bt, TypeZone tz)
		{
			Hashtable callers = tz.callers;
			
			tz.count += bt.LastObjectStats.AllocatedCount;
			tz.bytes += bt.LastObjectStats.AllocatedTotalBytes;
		
			foreach (Frame f in bt.Frames) {
				if (f.MethodName.StartsWith ("(wrapper"))
					continue;

				// Do nothing for now if we can't
				// find our method, but throw a fit
				if (!callers.ContainsKey (f.MethodName)) {
					Console.WriteLine ("Cannot find {0}", f.MethodName);
					continue;
				}
				
				((TypeZone)callers[f.MethodName]).count += bt.LastObjectStats.AllocatedCount;
				((TypeZone)callers[f.MethodName]).bytes += bt.LastObjectStats.AllocatedTotalBytes;
				
				callers = ((TypeZone)callers[f.MethodName]).callers;
			}		
		}
		
		public void AddBacktrace (Backtrace bt, TypeZone tz)
		{
			// Make sure the calls are in the data structures
			MergeBacktrace (bt, tz);
			
			// Add the new values to the calls
			UpdateBacktrace (bt, tz);
		}

		public void GetZoneData (OutfileReader reader)
		{
			types = new Hashtable ();
			TypeZone zone;
			
			foreach (Backtrace bt in reader.Backtraces) {
				if (types.ContainsKey (bt.Type.Name)) {
					zone = (TypeZone)types[bt.Type.Name];
				} else {
					zone = new TypeZone (bt.Type.Name);
					types.Add (bt.Type.Name, zone);
				}

				AddBacktrace (bt, zone);
				prog_bytes += zone.bytes;
			}
			
			uint [] sizes = new uint [types.Count];
			TypeZone [] list = new TypeZone [types.Count];
			uint i = 0;
		
			foreach (DictionaryEntry de in types) {
				TypeZone z = (TypeZone)de.Value;
				//Console.WriteLine ("{0} of type {1}", Util.PrettySize (z.bytes), z.type);
	
				sizes[i] = z.bytes;
				list[i]  = z;
				i++;
			}
			
			Array.Sort (sizes, list);
			Array.Reverse (list);
			
			typeArray = (Array)list.Clone ();
			
			/*
			foreach (TypeZone z in list) {
				Console.WriteLine ("{0} of type {1}", Util.PrettySize (z.bytes), z.type);
				PrintCallTrace (z, " ");
				//if (++i > 0)
					//break;
			}
			*/
		}
		
		/*
		 * Set's the path given a path builder string
		 */
		public void SetPath (string path) {
		 	CurrentPath = path;
		 	
		 	// Set the leading slash
		 	if (!CurrentPath.StartsWith ("/"))
		 		CurrentPath = "/" + CurrentPath;
		 		
		 	CurrentPath = CurrentPath.Replace ("//", "/");
		}
		
		/*
		 * Searches the data tree and returns a
		 * TypeZone that meets the path's spec
		 */
		public TypeZone GetTypeZoneFromPath (string path)
		{
			string [] segments = path.Split ('/');
			Hashtable ht = types;
			TypeZone tz = null;
			
			if (path == "/")
				return null;
					
			foreach (string s in segments) {
				if (s == "")
					continue;
					
				tz = (TypeZone)ht[s];
				if (tz == null) {
					Console.WriteLine ("No Match for {0}", s);
					return null;
				}
					
				ht = tz.callers;
			}
			
			CurrentPath = path;
			Console.WriteLine ("{0} with {1}", tz.name, Util.PrettySize (tz.bytes));
			
			return tz;
		}
	
		public void PrintCallTrace (TypeZone z, string indent)
		{
			TypeZone zone;
		
			foreach (DictionaryEntry de in z.callers) {
				zone = (TypeZone)de.Value;
				
				Console.Write (indent);
				Console.WriteLine ("{0} from {1} allocations - {2}", Util.PrettySize (zone.bytes), zone.count, de.Key);

				PrintCallTrace ((TypeZone)de.Value, indent + " ");
			}
		}
		
		/*
		 * Prints a numbered list of the
		 * calling functions for the given
		 * TypeZone
		 */
		public void PrintCallers (TypeZone z)
		{
			Table table = new Table ();
			table.AddHeaders (" # ", "Size", " % ", "Method");
			int i = 0;
			
			TypeZone tz;
			CurrentList = new SortedList ();
		
			foreach (DictionaryEntry de in z.callers) {
				tz = (TypeZone)de.Value;
				CurrentList.Add (i, tz.name);
				table.AddRow (i++ + " :", Util.PrettySize (tz.bytes), String.Format ("{0:#0.0}", (float)tz.bytes / (float)z.bytes * 100), de.Key);
			}
			
			table.Sort (2, false);
			
			Console.WriteLine (table);
		}
		
		/*
		 * THE BIG LIST FUNCTION
		 */
		public void List () {
			// Are we dealing with a method or a type?
			// TODO: Add the method and type distinction...
			
			if (CurrentPath == "/") {
				ListTypes ();
				return;
			}
			
			if (CurrentZone == null)
				CurrentZone = GetTypeZoneFromPath (CurrentPath);
			if (CurrentZone == null)
				return;

			ListTypeZone (CurrentZone);
		}
		
		public void ListTypeZone (TypeZone tz) {
			Table table = new Table ();
			int i = 0;
			
			table.AddHeaders (" # ", "Size", "Caller");
			
			foreach (DictionaryEntry de in tz.callers) {
				TypeZone z = (TypeZone)de.Value;
				
				if (max_rows >= 0 && i >= max_rows)
					break;
				
				table.AddRow (i++ + " :", Util.PrettySize (z.bytes), z.name);
			}
			
			Console.WriteLine (table);
		}
		
		public void ListTypes ()
		{
			Table table = new Table ();
			int i = 0;
			
			table.AddHeaders (" # ", "Size", " % ", "Type");
		
			foreach (TypeZone z in typeArray) {
				if (max_rows >= 0 && i >= max_rows)
					break;
				
				table.AddRow (i++ + " :", Util.PrettySize (z.bytes), String.Format ("{0:#0.0}", (float)z.bytes / (float)prog_bytes * 100), z.name);
			}
			
			Console.WriteLine (table);
		}
		
		public void ShowType (string f)
		{
			if (!types.ContainsKey (f))
				return;
			
			TypeZone t = (TypeZone)types[f];
			
			Console.WriteLine ("{0} of {1}", Util.PrettySize (t.bytes), t.name);
			PrintCallers (t);
			//PrintCallTrace (t, " ");	
		}
		
		public void ShowPath () {
			Console.WriteLine (CurrentPath);
		}
		
		public void PrintHelpInfo ()
		{
			Console.WriteLine ("Memlog commands:");
			Console.WriteLine ("  list: list the items in the current path");
			Console.WriteLine ("  show: preview the items in a listed item");
			Console.WriteLine ("  help: show this screen");
			Console.WriteLine ("  quit: quit");
		}
		
		override public void Run (OutfileReader reader, string [] args)
		{
			string cmd = "";
			string [] cmds;
		
			GetZoneData (reader);
			
			PrintHelpInfo ();
			
			while (String.Compare (cmd, "quit") != 0 && String.Compare (cmd, "q") != 0) {
				Console.Write ("{0}", PS1);
				
				cmd = Console.ReadLine ();
				if (cmd == null)
					cmd = "q";
				if (cmd == "")
					continue;
				
				Console.WriteLine ();
				int val = new int ();
				int n = new int ();
			
				cmds = cmd.Split (null);
				Operand op = Operand.OP_NONE;
				
				int i = 0;
				max_rows = 10;
				while (i < cmds.Length) {
					string arg = cmds [i].ToLower ();

					if (arg == "list" || arg == "lsit")
						op = Operand.OP_LIST;

					else if (arg == "path" || arg == "pth") {
						if (i + 1 >= cmds.Length)
							ShowPath ();
						else {						
							if (cmds[i+1].Split ('/').Length == 1 && !cmds[i+1].StartsWith ("/"))
								cmds[i+1] = CurrentPath + "/" + cmds[i+1];
														
							CurrentZone = GetTypeZoneFromPath (cmds[++i]);
							SetPath (cmds[i]);
						}
					}

					else if (arg == "all")
						max_rows = -1;
						
					else if (arg == "show" || arg == "shwo" || arg == "shw") {
						if (cmds[i+1] == "path") {
							ShowPath ();
							i++;
						} else					
							op = Operand.OP_SHOW;
					}
					else if (arg == "select" || arg == "slct" || arg == "slc" || arg == "slect")
						op = Operand.OP_SELECT;
						
					else {
						n = -1;
						try {
							n = Int32.Parse (arg);
						} catch { }
						if (n > 0)
							val = n;
					}
					++i;
				}
				
				switch (op) {

				case Operand.OP_LIST:
					if (n > 0)
						max_rows = val;
					List ();
					break;

				case Operand.OP_SHOW:
					if (val > typeArray.Length || val < 0)
						break;
					if (max_rows == -1) {
						foreach (TypeZone t in typeArray) {
							ShowType (t.name);
						}
					} else {
						ShowType (((TypeZone)typeArray.GetValue(val)).name);
					}
					break;

				case Operand.OP_SELECT:
					if (val > CurrentList.Count || val < 0)
						break;
					SetPath (CurrentPath + "/" + CurrentList[val]);
					Console.WriteLine (CurrentPath);
					break;

				case Operand.OP_HELP:
					PrintHelpInfo ();
					break;

				default:
					break;
				}
			}
		}
	}
}
