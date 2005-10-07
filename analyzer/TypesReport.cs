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


		override public void Run (OutfileReader reader, string [] args)
		{
			Table table;
			table = new Table ();

			table.AddHeaders ("Type",
					  "#",
					  "Total",
					  "AvSz",
					  "AvAge",
					  "BT#");
			
			table.SetStringify (0, Util.Ellipsize);
			table.SetStringify (2, Util.PrettySize_Obj);
			table.SetStringify (3, "0.0");
			table.SetStringify (4, "0.0");

			foreach (Type type in reader.Types) {
				table.AddRow (type.Name,
					      type.LastObjectStats.AllocatedCount,
					      type.LastObjectStats.AllocatedTotalBytes,
					      type.LastObjectStats.AllocatedAverageBytes,
					      type.LastObjectStats.AllocatedAverageAge,
					      type.BacktraceCount);
			}

			table.Sort (2, false);
			table.MaxRows = 25;

			Console.WriteLine (table);

			if (table.RowCount > table.MaxRows) {
				Console.WriteLine ();
				Console.WriteLine ("(skipped {0} types)", table.RowCount - table.MaxRows);
			}
		}
	}
}
