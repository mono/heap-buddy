//
// TypesReport.cs
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

			foreach (Type type in reader.GetTypesMatching (match_string)) {
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
