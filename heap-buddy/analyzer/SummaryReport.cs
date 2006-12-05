//
// SummaryReport.cs
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
