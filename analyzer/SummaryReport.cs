//
// SummaryReport.cs
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

	public class SummaryReport : Report {

		public SummaryReport () : base ("Summary") { }

		override public void Run (OutfileReader reader, string [] args)
		{
			Table table;
			table = new Table ();

			table.AddRow ("SUMMARY", "");
			table.AddRow ("", "");

			table.AddRow ("Filename:", reader.Filename);
			table.AddRow ("Allocated Bytes:", Util.PrettySize (reader.TotalAllocatedBytes));
			table.AddRow ("Allocated Objects:", reader.TotalAllocatedObjects);
			table.AddRow ("GCs:", reader.Gcs.Length);
			table.AddRow ("Resizes:", reader.Resizes.Length);
			table.AddRow ("Final heap size:", Util.PrettySize (reader.LastResize.NewSize));

			table.AddRow ("", "");

			table.AddRow ("Distinct Types:", reader.Types.Length);
			table.AddRow ("Backtraces:", reader.Backtraces.Length);

			table.SetAlignment (1, Alignment.Left);
			
			Console.WriteLine (table);
		}
	}
}
