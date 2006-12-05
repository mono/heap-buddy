//
// TypesReport.cs
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

namespace HeapBuddy {

	public class TypesReport : Report {

		public TypesReport () : base ("Types") { }

		enum SortOrder {
			Unsorted,
			ByCount,
			ByTotalBytes,
			ByAverageBytes,
			ByAverageAge,
			ByBacktraceCount,
		}

		override public void Run (OutfileReader reader, string [] args)
		{
			SortOrder order = SortOrder.ByTotalBytes;
			int max_rows = 25;
			string match_string = null;
			bool ellipsize_names = true;

			// Hacky free-form arg parser

			int i = 0;
			while (i < args.Length) {
				string arg = args [i].ToLower ();

				if (arg == "count")
					order = SortOrder.ByCount;
				else if (arg == "total")
					order = SortOrder.ByTotalBytes;
				else if (arg == "average")
					order = SortOrder.ByAverageBytes;
				else if (arg == "age")
					order = SortOrder.ByAverageAge;
				else if (arg == "backtrace" || arg == "backtraces" || arg == "bt")
					order = SortOrder.ByBacktraceCount;
				else if (arg == "all")
					max_rows = -1;
				else if (arg == "match" || arg == "matching" || arg == "like") {
					++i;
					match_string = args [i];
				} else if (arg == "full" || arg == "long" || arg == "unellipsized")
					ellipsize_names = false;
				else {
					int n = -1;
					try {
						n = Int32.Parse (arg);
					} catch { }
					if (n > 0)
						max_rows = n;
				}

				++i;
			}

			// Generate the table

			Table table;
			table = new Table ();

			table.AddHeaders ("Type",
					  "#",
					  "Total",
					  "AvSz",
					  "AvAge",
					  "BT#");
			
			if (ellipsize_names)
				table.SetStringify (0, Util.Ellipsize);
			table.SetStringify (2, Util.PrettySize_Obj);
			table.SetStringify (3, "0.0");
			table.SetStringify (4, "0.0");

			foreach (Type type in reader.Types) {
				if (match_string != null && ! type.Matches (match_string))
					continue;
				table.AddRow (type.Name,
					      type.LastObjectStats.AllocatedCount,
					      type.LastObjectStats.AllocatedTotalBytes,
					      type.LastObjectStats.AllocatedAverageBytes,
					      type.LastObjectStats.AllocatedAverageAge,
					      type.BacktraceCount);
			}

			switch (order) {
			case SortOrder.ByCount:
				table.Sort (1, false);
				break;
			case SortOrder.ByTotalBytes:
				table.Sort (2, false);
				break;
			case SortOrder.ByAverageBytes:
				table.Sort (3, false);
				break;
			case SortOrder.ByAverageAge:
				table.Sort (4, false);
				break;
			case SortOrder.ByBacktraceCount:
				table.Sort (5, false);
				break;
			}

			if (max_rows > 0)
				table.MaxRows = max_rows;

			Console.WriteLine (table);

			if (table.RowCount > table.MaxRows) {
				Console.WriteLine ();
				Console.WriteLine ("(skipped {0} types)", table.RowCount - table.MaxRows);
			}
		}
	}
}
