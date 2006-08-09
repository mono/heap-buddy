using System;
using System.Collections;
using Cairo;
using Gtk;

namespace HeapBuddy {
	
	public class MemGraph {
	
		private class MemStamp {
			public long LiveBytes;
			public long TimeT;
			
			public MemStamp (long bytes, long time) {
				LiveBytes = bytes;
				TimeT = time;
			}
		}
		
		private class MemStampComparer : IComparer {
			int IComparer.Compare (System.Object x, System.Object y) {
				MemStamp a = (MemStamp)x;
				MemStamp b = (MemStamp)y;
			
				if (a.TimeT > b.TimeT) return 1;
				else if (a.TimeT < b.TimeT) return -1;
				else return 0;
			}
		}
		
		private ArrayList Stamps;
		static DrawingArea da;

		public MemGraph (OutfileReader reader, string filename)
		{	
			Stamps = new ArrayList ();
			CollectStamps (reader);
			Sort ();
			
			// If we don't have any data, bail
			if (Stamps.Count <= 0)
				return;
			
			const int SurfaceWidth = 640;
			const int SurfaceHeight = 480;
			
			// Generate our Cairo surface
			ImageSurface surface = new ImageSurface (Format.RGB24, SurfaceWidth, SurfaceHeight);
			Context c = new Context (surface);
			
			c.Color = new Color (1, 1, 1, 1);
			c.Paint ();
			
			// Calculate our bounds
			long domain = ((MemStamp)Stamps [Stamps.Count - 1]).TimeT - ((MemStamp)Stamps [0]).TimeT;
			if (domain == 0)
				return;
				
			long lowBytes = ((MemStamp)Stamps [0]).LiveBytes;
			long highBytes = 0;
			
			foreach (MemStamp ms in Stamps) {
				if (ms.LiveBytes < lowBytes)
					lowBytes = ms.LiveBytes;
				if (ms.LiveBytes > highBytes)
					highBytes = ms.LiveBytes;
			}
				
			const double GraphWidth = 570;
			const double GraphHeight = 420;
			const double GraphOriginX = SurfaceWidth - GraphWidth - 15;
			const double GraphOriginY = 15;
				
			// We need to scale this puppy...
			double xscale = (double)domain / GraphWidth;
			double yrange = highBytes - lowBytes;
			double yscale = yrange / GraphHeight;
			
			//Matrix m = c.Matrix;
			//m.Scale (xscale, yscale);
			//c.Matrix = m;
			
			c.Color = new Color (0, 0, 0, 1);
			
			c.LineWidth = 2;			
			c.Rectangle (GraphOriginX, GraphOriginY, GraphWidth, GraphHeight);
			
			c.MoveTo (GraphOriginX, GraphOriginY + GraphHeight);
			long lowTime = ((MemStamp)Stamps [0]).TimeT;
			foreach (MemStamp ms in Stamps) {
				c.LineTo (GraphOriginX + (double)(ms.TimeT - lowTime) / xscale, GraphOriginY + GraphHeight - (double)(ms.LiveBytes - lowBytes) / yscale);
			}
			c.LineWidth = 1.5;
			c.Stroke ();
			
			// Draw the Memory Text
			// Tick Marks...
			c.FontSize = 15;
			c.LineWidth = 1;

			for (int i = 0; i <= 10; i++) {
				c.MoveTo (GraphOriginX - 5, GraphOriginY + GraphHeight - i * GraphHeight / 10);
				c.LineTo (GraphOriginX, GraphOriginY + GraphHeight - i * GraphHeight / 10);
				c.Stroke ();

				string s = Util.PrettySize (lowBytes + (i * (long)yrange / 10));
				TextExtents e = c.TextExtents (s);
				c.MoveTo (GraphOriginX - e.Width - 10, GraphOriginY + GraphHeight - i * GraphHeight / 10 + 0.5 * e.Height);
				c.ShowText (s);
			}
			
			// Draw the time Text
			for (int i = 0; i < 15; i++) {
				c.MoveTo (GraphOriginX + i * GraphWidth / 10, GraphOriginY + GraphHeight);
				c.LineTo (GraphOriginX + i * GraphWidth / 10, GraphOriginY + GraphHeight + 5);
				c.Stroke ();
				
				string s = Util.PrettyTime (i * domain / 10);
				TextExtents e = c.TextExtents (s);
				c.MoveTo (GraphOriginX + i * GraphWidth / 10 - 0.5 * e.Width, GraphOriginY + GraphHeight + 10 + e.Height);
				c.ShowText (s);
			}
			
			Application.Init ();
			
			Window win = new Window ("Heap-Buddy");
			win.SetDefaultSize (640, 480);
			
			da = new CairoGraph ();
			win.Add (da);
			
			win.ShowAll ();
			
			Application.Run ();			
				
			if (filename == null)
				filename = "memlog.png";
				
			surface.WriteToPng (filename);
			surface.Finish ();
		}
		

		
		private void CollectStamps (OutfileReader reader)
		{
			foreach (Gc gc in reader.Gcs) {
				Stamps.Add (new MemStamp (gc.PostGcLiveBytes, gc.TimeT));
			}
			
			foreach (Resize r in reader.Resizes) {
				Stamps.Add (new MemStamp (r.TotalLiveBytes, r.time_t));
			}
		}

		private void Sort ()
		{
			IComparer ic = new MemStampComparer ();
			Stamps.Sort (ic);
		}
			
	}
	
}

public class CairoGraph : DrawingArea
{
	protected override bool OnExposeEvent (Gdk.EventExpose args)
	{
		Gdk.Window win = args.Window;
		
		Cairo.Context g = Gdk.Context.CreateDrawable (win);
	
		g.ResetClip ();
		g.Color = new Color (0, .6, .6, 1);
		g.Paint ();
		
		g.Color = new Color (1, 1, 1, 1);
		g.MoveTo (0, 0);
		g.LineTo (500, 500);
		g.LineWidth = 4;
		g.Stroke ();
		
		return true;
	}
}
