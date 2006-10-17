//
// MemlogReport.cs
// based on BacktracesReport.cs
//

//
// BacktracesReport.cs
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
using System.IO;
using System.Text;

namespace HeapBuddy {

	public class MemlogReport : Report {
	  const string     PS1 = ">> ";
	  public int       MaxRows = 10;
		
		public TypeLog   TypeLog;
		public MemZone   Types;

		public MethodLog MethodLog;
		public MemZone   Methods;
		
		public string    CurrentPath;
		public MemZone   CurrentZone;
		
		enum Operand {
			OP_NONE,
			OP_LIST
		}
		
		public MemlogReport () : base ("Memlog") { }

		/*
		 * Set's the path given a path builder string
		 */
		public void SetPath (string path) {
			if (path == "/") {
				// Go to the root
				CurrentPath = path;
				CurrentZone = null;
				
				return;
				
			} else if (path[0] == '/') {
		 		// Absolute Path
		 		string [] segments = path.Split ('/');
		 				 				 		
		 		if (segments[1] != "types" && segments[1] != "methods")
		 			throw new ArgumentException ();
		 			
		 		CurrentPath = path;
		 		
		 	} else if (path == "up") {
		 		int index  = CurrentPath.LastIndexOf ('/');
		 		int length = CurrentPath.Length;
		 		
		 		CurrentPath = CurrentPath.Remove (index, length - index);
		 		
		 	} else {
		 		// Relative Path
		 		CurrentPath += "/" + path;
		 	
		 	}
		 	
		 	// Set the leading slash
		 	if (!CurrentPath.StartsWith ("/"))
		 		CurrentPath = "/" + CurrentPath;
		 			 		
		 	CurrentPath = CurrentPath.Replace ("//", "/");
		 	
		 	if (CurrentPath.Length > 1 && CurrentPath.EndsWith ("/"))
		 		CurrentPath = CurrentPath.Remove (CurrentPath.Length - 1, 1);

		 	CurrentZone = GetByPath (CurrentPath);
		 	if (CurrentZone == null)
		 		throw new ArgumentException ();
		}
		
		/*
		 * Searches the data tree and returns a
		 * MemZone that meets the path's spec
		 */
		public MemZone GetByPath (string path)
		{
			string [] segments = path.Split ('/');
			MemZone mz = null;
			
			if (path == "/")
				return null;
				
			// This is a relative path,
			// so we must set the initial
			// MemZone
			if (path[0] != '/') {
				mz = CurrentZone;
				
				if (mz == null)
					throw new ArgumentException ();
			}
					
			foreach (string s in segments) {
				switch (s) {
				
				case "types":
					mz = Types;
					break;
				
				case "methods":
					mz = Methods;
					break;
				
				case "":
					break;
				
				default:
					mz = mz[s];
					if (mz == null)
						throw new ArgumentException (s);
				
					break;
				}
			}
			
			return mz;
		}
	
		public void PrintCallTrace (MemZone mz, string indent)
		{
			foreach (MemZone z in mz.Methods) {
				Console.Write (indent);
				Console.WriteLine ("{0} from {1} allocations - {2}", Util.PrettySize (z.Bytes), z.Allocations, z.Name);

				PrintCallTrace (z, indent + " ");
			}
		}
		
		/*
		 * Prints the methods in a given
		 * MemZone's Methods array
		 *
		 * Prints MaxRows rows
		 */
		public void PrintMethods (MemZone mz)
		{
			Table table = new Table ();
			table.AddHeaders (" # ", "Size", " % ", "Tot %", "Count", "Method");
			int i = 0;
			uint bytes = 0;
			
			if (CurrentPath == "/") {
				PrintCategories ();
				return;
			}
								
			foreach (MemZone z in mz.Methods) {
				if (MaxRows > 0 && i >= MaxRows)
					break;
			
				table.AddRow (i++ + " :",
				  Util.PrettySize (z.Bytes),
				  String.Format ("{0:#0.0}", (float)z.Bytes / (float)mz.Bytes * 100),
				  String.Format (" {0:#0.000} ", (float)z.Bytes / (float)Types.Bytes * 100),
				  z.Allocations,
				  z.Name);
				  
				bytes += z.Bytes;
			}
			
			Console.WriteLine (table);
			//if (mz.Name != null && mz.Name.IndexOf(':') != -1)
			//	Console.WriteLine ("\n{0} in Current Item: {1}", Util.PrettySize (mz.Bytes - bytes), mz.Name);
		}
		
		public void ShowPath () {
			Console.WriteLine (CurrentPath);
		}
		
		public void PrintHelpInfo ()
		{
			Console.WriteLine ("Memlog commands:");
			Console.WriteLine ("  list: list the items in the current path");
			Console.WriteLine ("  rows [n]: specify how many rows to print - zero for all");
			Console.WriteLine ("  help: show this screen");
			Console.WriteLine ("  quit: quit");
		}
		
		public void PrintCategories () {
			Console.WriteLine ("0 : types");
			Console.WriteLine ("1 : methods");
		}
		
		public void Blert (string s) {
			Console.WriteLine (" ~{0}~", s);
		}
		
		public void Init (OutfileReader reader) {
			TypeLog   = new TypeLog   (reader);
			MethodLog = new MethodLog (reader);
			
			Types     = TypeLog.Types;
			Methods   = MethodLog.Methods;
			
			CurrentPath = "/";
			CurrentZone = null;
			
			PrintHelpInfo ();
		}
		
		override public void Run (OutfileReader reader, string [] args)
		{
			string cmd = "";
			string [] cmds;
			
			Init (reader);
			
			while (String.Compare (cmd, "quit") != 0 && String.Compare (cmd, "q") != 0) {
				Console.Write ("{0}", PS1);
				
				cmd = Console.ReadLine ();
				if (cmd == null)
					cmd = "q";
				if (cmd == "")
					continue;
				
				int n = new int ();
			
				cmds = cmd.Split (null);
				Operand op = Operand.OP_NONE;
				
				int i = 0;
				while (i < cmds.Length) {
					string arg = cmds [i].ToLower ();
					
					switch (arg) {
					
					case "help":
						PrintHelpInfo ();
						break;
											
					case "list":case "lsit":case "ls":case "l":
						op = Operand.OP_LIST;
					
						break;
					
					case "rows":case "rosw":case "rose":
						n = -1;
						try {
							n = Int32.Parse (cmds [i+1]);
						} catch { }
						
						if (n >= 0) {
							MaxRows = n;
							i++;
						}
					
						break;
						
					case "/":
						SetPath ("/");
						break;
						
					case "../":case "..":case "up":
						try {
							SetPath ("up");
						} catch { }
						
						PrintMethods (CurrentZone);
						
						break;
						
					case "path":case "pth":
						if (i + 1 >= cmds.Length)
							ShowPath ();
						else {						
							try {
								SetPath (cmds[++i]);
								
								PrintMethods (CurrentZone);
							} catch (ArgumentException) {
								Blert ("Invalid Path");
							}
						}
						
						break;
						
					default:
										
						if (i > 0)
							break;
					
						// Check for the user selecting
						// a numbered item
						
						n = -1;
						try {
							n = Int32.Parse (arg);
						} catch { }
						
						if (n >= 0) {
						
							if (CurrentPath == "/") {
							
								if (n == 0)
									SetPath ("/types");
								else if (n == 1)
									SetPath ("/methods");
								else {
									Blert ("Invalid Category");
									return;
								}
									
							} else {
							
								if (CurrentZone [n] == null) {
									Blert ("Invalid Selection");
									break;
								}	else {
									try {
										SetPath (CurrentZone [n].Name);
									} catch { }
								}
							}
							
						}
						
						PrintMethods (CurrentZone);
						
						break;
					}

					++i;
				}
				
				switch (op) {

				case Operand.OP_LIST:
					string category = ((string [])CurrentPath.Split ('/'))[1];
										
					if (category != "types" && category != "methods")
						PrintCategories ();
					else
						try {
							PrintMethods (CurrentZone);
						} catch { }

					break;

				default:
					break;
				}
			}
		}
	}
}
