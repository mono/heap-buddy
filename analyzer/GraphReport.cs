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
using Glade;

namespace HeapBuddy {

	public class GraphReport : Report {
		
		public GraphReport () : base ("Graph") { }

		public Context Context;
		public Gdk.Window Window;
		
		public OutfileReader Reader;
		public ArrayList Stamps;
		
		[Widget] VBox vbox1;
		[Widget] Gtk.Window MainWindow;
		
		override public void Run (OutfileReader reader, string [] args)
		{
			Reader = reader;
			Stamps = new ArrayList ();
			CollectStamps ();
			Sort ();
					
			Application.Init ();
			
			Glade.XML gxml = new Glade.XML (null, "memgraph.glade", "MainWindow", null);
			gxml.Autoconnect (this);

			CairoGraph cg = new CairoGraph (this);
			vbox1.Add (cg);
			
			MainWindow.ShowAll ();
			MainWindow.Resize (640, 480);
			MainWindow.DeleteEvent += QuitApplication;
						
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
			c.FontSize = 15 * w / 640;
			if (15 * w / 640 > 20)
				c.FontSize = 20;

			string label = Util.PrettySize (HighBytes);
			double GOY = h - c.TextExtents (label).Height - 30;
			
			c.FontSize = 15 * h / 480;
			if (15 * h / 480 > 20)
				c.FontSize = 20;
				
			double GOX = c.TextExtents (label).Width + 15;
			double GW = w - GOX - 10;
			double GH = GOY - 10;
			
			double xscale = (double)TimeSpan / GW;
			double yrange = HighBytes - LowBytes;
			double yscale = yrange / GH;
			
			// Border
			c.Color = new Color (0, 0, 0, 1);
			c.LineWidth = 2;
			c.Rectangle (GOX, GOY, GW, -GH);
			c.Stroke ();
			
			// Memory line
			c.MoveTo (GOX, GOY);
			long LowTime = ((MemStamp)Stamps [0]).TimeT;
			
			Color Black       = new Color (0, 0, 0, 1);
			Color GcColor     = new Color (0, 0, 1, 1);
			Color ResizeColor = new Color (1, 0, 0, 1);
			
			double oldX = GOX;
			double oldY = GOY;
			double newX, newY;
			
			c.LineWidth = 1.5;
			foreach (MemStamp ms in Stamps) {
				switch (ms.Op) {
					case MemAction.Gc:
						c.Color = GcColor;
						break;
						
					case MemAction.Resize:
						c.Color = ResizeColor;
						break;
						
					default:
						c.Color = Black;
						break;
				}
				
				newX = GOX + (double)(ms.TimeT - LowTime) / xscale;
				newY = GOY - (double)(ms.LiveBytes - LowBytes) / yscale;
				
				c.MoveTo (oldX, oldY);
				c.LineTo (newX, newY);
				c.Stroke ();
				
				oldX = newX;
				oldY = newY;
			}
			c.Color = Black;
			
			// Labels
			c.LineWidth = 1;
			c.FontSize = 15 * h / 480;
			if (15 * h / 480 > 20)
				c.FontSize = 20;
			
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
			c.FontSize = 15 * w / 640;
			if (15 * w / 640 > 20)
				c.FontSize = 20;
			
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
		
		public enum MemAction {
			NoOp,
			Gc,
			Resize
		}
		
		public class MemStamp {
			public long LiveBytes;
			public long TimeT;
			public MemAction Op;
			
			public MemStamp (long bytes, long time) {
				LiveBytes = bytes;
				TimeT = time;
				Op = MemAction.NoOp;
			}
			
			public MemStamp (long bytes, long time, MemAction op) {
				LiveBytes = bytes;
				TimeT = time;
				Op = op;
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
				Stamps.Add (new MemStamp (gc.PostGcLiveBytes, gc.TimeT, MemAction.Gc));
			}
			
			foreach (Resize r in Reader.Resizes) {
				Stamps.Add (new MemStamp (r.TotalLiveBytes, r.time_t, MemAction.Resize));
			}
		}

		public void Sort ()
		{
			IComparer ic = new MemStampComparer ();
			Stamps.Sort (ic);
		}
		
		protected void SaveGraph (object o, EventArgs e)
		{
			FileChooserDialog chooser = new FileChooserDialog ("Save As", MainWindow, FileChooserAction.Save);
			chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
			chooser.AddButton (Stock.Save, ResponseType.Ok);
			
			int response = chooser.Run ();
			
			if ((ResponseType)response == ResponseType.Ok) {
				int x, y, w, h, d;
				Window.GetGeometry (out x, out y, out w, out h, out d);
				
				Surface s = new ImageSurface (Format.RGB24, w, h);
				Context.Target = s;
				this.Continue ();

				s.WriteToPng (chooser.Filename);
				s.Finish ();
			}
			
			chooser.Destroy ();
		}
	
		protected void QuitApplication (object o, EventArgs e)
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
