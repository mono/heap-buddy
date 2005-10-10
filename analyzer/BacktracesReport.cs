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

	public class BacktracesReport : Report {

		public BacktracesReport () : base ("Backtraces") { }

		enum SortOrder {
			Unsorted,
			ByCount,
			ByTotalBytes,
			ByAverageBytes,
			ByAverageAge
		}

		static string BacktraceStringifier (Backtrace bt, int max_width)
		{
			StringBuilder sb = new StringBuilder ();

			sb.Append ("type=");
			sb.Append (Util.Ellipsize (max_width-5, bt.Type.Name));
			foreach (Frame frame in bt.Frames) {
				if (! frame.MethodName.StartsWith ("(wrapper")) {
					sb.Append ('\n');
					sb.Append (Util.Ellipsize (max_width, frame.MethodName));
				}
			}

			return sb.ToString ();
		}

		static string BacktraceStringifier_Full (object obj)
		{
			return BacktraceStringifier ((Backtrace) obj, -1); 
		}

		static string BacktraceStringifier_Ellipsize (object obj)
		{
			const int max_width = 51;
			return BacktraceStringifier ((Backtrace) obj, max_width); 
		}

		override public void Run (OutfileReader reader, string [] args)
		{
			SortOrder order = SortOrder.ByTotalBytes;
			int max_rows = 25;
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
				else if (arg == "all")
					max_rows = -1;
				else if (arg == "full" || arg == "long" || arg == "unellipsized")
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

			table.AddHeaders ("Backtrace",
					  "#",
					  "Total",
					  "AvSz",
					  "AvAge");
			
			if (ellipsize_names)
				table.SetStringify (0, BacktraceStringifier_Ellipsize);
			else
				table.SetStringify (0, BacktraceStringifier_Full);
			table.SetStringify (2, Util.PrettySize_Obj);
			table.SetStringify (3, "0.0");
			table.SetStringify (4, "0.0");

			foreach (Backtrace bt in reader.Backtraces) {
				table.AddRow (bt,
					      bt.LastObjectStats.AllocatedCount,
					      bt.LastObjectStats.AllocatedTotalBytes,
					      bt.LastObjectStats.AllocatedAverageBytes,
					      bt.LastObjectStats.AllocatedAverageAge);
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
			}

			table.SkipLines = true;
			if (max_rows > 0)
				table.MaxRows = max_rows;

			Console.WriteLine (table);

			if (table.RowCount > table.MaxRows) {
				Console.WriteLine ();
				Console.WriteLine ("(skipped {0} backtraces)", table.RowCount - table.MaxRows);
			}
		}
	}
}
