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
			Resize [] resizes;
			resizes = reader.Resizes;

			Gc [] gcs;
			gcs = reader.Gcs;

			int i_resize = 0;
			int i_gc = 0;

			while (i_resize < resizes.Length || i_gc < gcs.Length) {
				
				Resize r = null;
				if (i_resize < resizes.Length)
					r = resizes [i_resize];
				
				Gc gc = null;
				if (i_gc < gcs.Length)
					gc = gcs [i_gc];

				if (r != null && (gc == null || r.Generation <= gc.Generation)) {
					Console.WriteLine ("{0:HH:mm:ss} | Resize | {1} -> {2}, {3} in live objects",
							   r.Timestamp,
							   Util.PrettySize (r.PreviousSize),
							   Util.PrettySize (r.NewSize),
							   Util.PrettySize (r.TotalLiveBytes));
					++i_resize;
				} else if (gc != null) {
					if (gc.Generation >= 0) {
						Console.WriteLine ("{0:HH:mm:ss} | GC {1:000} | {2} -> {3}, freed {4} ({5:0.0}%)",
								   gc.Timestamp,
								   gc.Generation,
								   Util.PrettySize (gc.PreGcLiveBytes),
								   Util.PrettySize (gc.PostGcLiveBytes),
								   Util.PrettySize (gc.FreedBytes),
								   gc.FreedPercentage);
					}
					++i_gc;
				}
			}
		}
	}
}
