//
// Graph.cs
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
using Cairo;
using Gtk;

namespace HeapBuddy {

	public class GraphReport : Report {
		
		public GraphReport () : base ("Graph") { }

		public Context Context;
		public Gdk.Window Window;
		
		public OutfileReader Reader;
		
		public ArrayList Stamps;
		
		override public void Run (OutfileReader reader, string [] args)
		{
			Reader = reader;
			Stamps = new ArrayList ();
			CollectStamps ();
			Sort ();
		
			Application.Init ();
			
			Window MainWindow = new Window ("Heap-Buddy");
			MainWindow.SetDefaultSize (640, 480);
			MainWindow.DeleteEvent += QuitApplication;
			
			VPaned box1 = new VPaned ();
			MainWindow.Add (box1);
			
			MenuBar MainMenu = new MenuBar ();
			Menu FileMenu = new Menu ();
			MenuItem ExitItem = new MenuItem ("E_xit");
			ExitItem.Activated += QuitApplication;
			FileMenu.Append (ExitItem);
			MenuItem FileItem = new MenuItem ("_File");
			FileItem.Submenu = FileMenu;
			MainMenu.Append (FileItem);	
			box1.Add (MainMenu);
			
			CairoGraph cg = new CairoGraph (this);
			box1.Add (cg);
			
			box1.ResizeChildren ();
			
			MainWindow.ShowAll ();			
			Application.Run ();
		}
		
		public void SetContext (Context c, Gdk.Window w)
		{
			Context = c;
			Window = w;
			
			Continue ();
		}
		
		public void Continue ()
		{		
			int x, y, w, h, d;
			Window.GetGeometry (out x, out y, out w, out h, out d);
			
			Context c = Context;
			
			if (Stamps.Count <= 0)
				return;
				
			c.Color = new Color (1, 1, 1, 1);
			c.Paint ();
			
			// Calculate our Time Span, bail if zero
			long TimeSpan = ((MemStamp)Stamps [Stamps.Count - 1]).TimeT - ((MemStamp)Stamps [0]).TimeT;
			if (TimeSpan == 0)
				return;
				
			long LowBytes = ((MemStamp)Stamps [0]).LiveBytes;
			long HighBytes = 0;
			
			foreach (MemStamp ms in Stamps) {
				if (ms.LiveBytes < LowBytes)
					LowBytes = ms.LiveBytes;
				if (ms.LiveBytes > HighBytes)
					HighBytes = ms.LiveBytes;
			}
			
			//*********Scaling
			
			// How much room for the labels?
			c.FontSize = 15;
			string label = Util.PrettySize (HighBytes);
			double GOX = c.TextExtents (label).Width + 15;
			double GOY = h - 30;
			double GW = w - GOX - 10;
			double GH = GOY - 10;
			
			double xscale = (double)TimeSpan / GW;
			double yrange = HighBytes - LowBytes;
			double yscale = yrange / GH;
			
			// Border
			c.Color = new Color (0, 0, 0, 1);
			c.LineWidth = 5;
			c.Rectangle (GOX, GOY, GW, -GH);
			
			// Memory line
			c.MoveTo (GOX, GOY);
			long LowTime = ((MemStamp)Stamps [0]).TimeT;
			foreach (MemStamp ms in Stamps) {
				c.LineTo (GOX + (double)(ms.TimeT - LowTime) / xscale, GOY - (double)(ms.LiveBytes - LowBytes) / yscale);
			}
			c.LineWidth = 1.5;
			c.Stroke ();
			
			// Labels
			c.LineWidth = 1;
			
			// Memory
			for (int i = 0; i <= 10; i++) {
				c.MoveTo (GOX - 5, GOY - i * GH / 10);
				c.LineTo (GOX, GOY - i * GH / 10);
				c.Stroke ();
				
				label = Util.PrettySize (LowBytes + (i * (long)yrange / 10));
				TextExtents e = c.TextExtents (label);
				c.MoveTo (GOX - 10 - e.Width, GOY - i * GH / 10 + 0.5 * e.Height);
				c.ShowText (label);
			}
			
			// Time
			for (int i = 0; i < 15; i++) {
				c.MoveTo (GOX + i * GW / 15, GOY);
				c.LineTo (GOX + i * GW / 15, GOY + 5);
				c.Stroke ();
				
				label = Util.PrettyTime (i * TimeSpan / 15);
				TextExtents e = c.TextExtents (label);
				c.MoveTo (GOX + i * GW / 15 - 0.5 * e.Width, GOY + 15 + e.Height);
				c.ShowText (label);
			}
		}
		
		public class MemStamp {
			public long LiveBytes;
			public long TimeT;
			
			public MemStamp (long bytes, long time) {
				LiveBytes = bytes;
				TimeT = time;
			}
		}
		
		public class MemStampComparer : IComparer {
			int IComparer.Compare (System.Object x, System.Object y) {
				MemStamp a = (MemStamp)x;
				MemStamp b = (MemStamp)y;
			
				if (a.TimeT > b.TimeT) return 1;
				else if (a.TimeT < b.TimeT) return -1;
				else return 0;
			}
		}
		
		public void CollectStamps ()
		{
			foreach (Gc gc in Reader.Gcs) {
				Stamps.Add (new MemStamp (gc.PostGcLiveBytes, gc.TimeT));
			}
			
			foreach (Resize r in Reader.Resizes) {
				Stamps.Add (new MemStamp (r.TotalLiveBytes, r.time_t));
			}
		}

		public void Sort ()
		{
			IComparer ic = new MemStampComparer ();
			Stamps.Sort (ic);
		}
	
		protected static void QuitApplication (object o, EventArgs e)
		{
			Application.Quit ();
		}
		
	}
		
	public class CairoGraph : DrawingArea
	{
		public Cairo.Context Context;
		public Gdk.Window Window;
		public GraphReport GraphReport;
		
		public CairoGraph (GraphReport gr)
		{
			GraphReport = gr;
		}
	
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Window  = args.Window;
			Context = Gdk.Context.CreateDrawable (Window);
			
			GraphReport.SetContext (Context, Window);
			
			return true;
		}
	}
}
