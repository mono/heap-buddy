//
// HistoryReport.cs
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

	public class HistoryReport : Report {

		public HistoryReport () : base ("History") { }

		override public void Run (OutfileReader reader, string [] args)
		{
			Table table;
			table = new Table ();
			table.Separator = " | ";

			Resize [] resizes;
			resizes = reader.Resizes;

			Gc [] gcs;
			gcs = reader.Gcs;

			int i_resize = 0;
			int i_gc = 0;
			long heap_size = 0;

			while (i_resize < resizes.Length || i_gc < gcs.Length) {
				
				Resize r = null;
				if (i_resize < resizes.Length)
					r = resizes [i_resize];
				
				Gc gc = null;
				if (i_gc < gcs.Length)
					gc = gcs [i_gc];

				if (i_resize != 0 || i_gc != 0)
					table.AddRow ("", "", "");

				string timestamp, tag, message;

				if (r != null && (gc == null || r.Generation <= gc.Generation)) {
					timestamp = string.Format ("{0:HH:mm:ss}", r.Timestamp);

					if (r.PreviousSize == 0) {
						tag = "Init";
						message = String.Format ("Initialized heap to {0}",
									 Util.PrettySize (r.NewSize));
					} else {
						tag = "Resize";
						message = String.Format ("Grew heap from {0} to {1}\n" +
									 "{2} in live objects\n" +
									 "Heap went from {3:0.0}% to {4:0.0}% capacity",
									 Util.PrettySize (r.PreviousSize),
									 Util.PrettySize (r.NewSize),
									 Util.PrettySize (r.TotalLiveBytes),
									 r.PreResizeCapacity, r.PostResizeCapacity);
					}

					heap_size = r.NewSize;
					++i_resize;

				} else {
					timestamp = String.Format ("{0:HH:mm:ss}", gc.Timestamp);
					if (gc.Generation >= 0) {
						tag = "GC " + gc.Generation;
						message = String.Format ("Collected {0} of {1} objects ({2:0.0}%)\n" +
									 "Collected {3} of {4} ({5:0.0}%)\n" +
									 "Heap went from {6:0.0}% to {7:0.0}% capacity",
									 gc.FreedObjects,
									 gc.PreGcLiveObjects,
									 gc.FreedObjectsPercentage,
									 Util.PrettySize (gc.FreedBytes),
									 Util.PrettySize (gc.PreGcLiveBytes),
									 gc.FreedBytesPercentage,
									 100.0 * gc.PreGcLiveBytes / heap_size,
									 100.0 * gc.PostGcLiveBytes / heap_size);
					} else {
						tag = "Exit";
						message = String.Format ("{0} live objects using {1}",
									 gc.PreGcLiveObjects,
									 Util.PrettySize (gc.PreGcLiveBytes));
					}
					++i_gc;
				}

				table.AddRow (timestamp, tag, message);
			}

			table.SetAlignment (1, Alignment.Left);
			table.SetAlignment (2, Alignment.Left);

			Console.WriteLine (table);
		}
	}
}
