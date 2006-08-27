using Cairo;
using Gtk;
using Mono.Unix.Native;
using System;
using System.Collections;

namespace HeapBuddy {

	public class MemStamp
	{
			public Timeval Time;
			public string Type;
			public long Bytes;
			
			public MemStamp (Timeval time, string type, long bytes)
			{
				Time  = time;
				Type  = String.Intern (type);
				Bytes = bytes;
			}

			public MemStamp (long time, long bytes)
			{
				Time  = new Timeval ();
				Time.tv_sec = time;
				Time.tv_usec = 0;
				Type  = null;
				Bytes = bytes;
			}
			
			public long Seconds {
				get {
					return Time.tv_sec;
				}
			}
			
			public long USeconds {
				get {
					return Time.tv_usec;
				}
			}
	}
	
	public class MemStampComparer : IComparer {
		int IComparer.Compare (System.Object x, System.Object y) {
			MemStamp a = (MemStamp)x;
			MemStamp b = (MemStamp)y;
			
			if (a.Seconds > b.Seconds)
				return 1;
			else if (a.Seconds < b.Seconds)
				return -1;
			else
			{
				if (a.USeconds > b.USeconds)
					return 1;
				else if (a.USeconds < b.USeconds)
					return -1;
				else
					return 0;
			}
		}
	}
	
	public class MemGraph : DrawingArea
	{
	
		private Context myContext;
		private ArrayList StampBook;
		private bool isSorted;
		private long HighTime = 0;
		private long LowTime= (uint)-1;
		private Gdk.Window myWindow;
		private double Scale = 1;
		private int x, y, w, h, d;
		
		public MemGraph ()
		{
			StampBook = new ArrayList ();
			isSorted = false;
			
			Events = Events | Gdk.EventMask.ButtonPressMask
			                | Gdk.EventMask.ButtonReleaseMask;
			ButtonPressEvent += ButtonPress;
			ButtonReleaseEvent += ButtonRelease;
		}
		
		public Cairo.Context Context {
			get {
				return myContext;
			}
		}
		
		public void AddStamp (MemStamp s)
		{
			StampBook.Add (s);
			isSorted = false;
			
			if (s.Seconds == 0)
				return;
			
			if (s.Type == null && s.Seconds < LowTime)
				LowTime = s.Seconds;
			
			if (s.Type == null && s.Seconds > HighTime)
				HighTime = s.Seconds;
		}
		
		public void Sort ()
		{
			IComparer ic = new MemStampComparer ();
			StampBook.Sort (ic);
		}
		
		public void Draw ()
		{
				if (!isSorted)
					Sort ();					

				Context c = myContext;

				Color Black = new Color (0, 0, 0, 1);
				Color White = new Color (1, 1, 1, 1);
				Color Red   = new Color (1, 0, 0, 1);
				Color Blue  = new Color (0, 0, 1, 1);
				
				c.Color = White;
				c.MoveTo (x - 50, y - 50);
				c.LineTo (x + w + 50, y - 50);
				c.LineTo (x + w + 50, y + h + 50);
				c.LineTo (x - 50, y + h + 50);
				c.Clip ();
				c.Fill ();
				c.Paint ();
				c.Stroke ();

				myWindow.GetGeometry (out x, out y, out w, out h, out d);

				long TimeSpan = HighTime - LowTime;
				if (TimeSpan == 0)
					return;
					
				long LowBytes = ((MemStamp)StampBook [0]).Bytes;
				long HighBytes = 0;
				
				foreach (MemStamp ms in StampBook) {
					if (ms.Type != null)
						continue;
				
					if (ms.Bytes < LowBytes)
						LowBytes = ms.Bytes;
					if (ms.Bytes > HighBytes)
						HighBytes = ms.Bytes;
				}
				
				double GOY;
				double GOX;
				double GW;
				double GH;
				double xscale;
				double yscale;
				double yrange;
				string label;
				
				c.Scale (Scale, Scale);
				
				c.FontSize = 15 * w / 640;
				if (15 * w / 640 > 20)
					c.FontSize = 20;
					
				label = Util.PrettySize (HighBytes);
				GOY = h - c.TextExtents (label).Height - 30;
				
				c.FontSize = 15 * h / 480;
				if (15 * h / 480 > 20)
					c.FontSize = 20;

				GOX = c.TextExtents (label).Width + 15;
				GW = w - GOX - 10;
				GH = GOY - 10;
				
				xscale = (double)TimeSpan / GW;
				yrange = HighBytes - LowBytes;
				yscale = yrange / GH;
				
				c.MoveTo (GOX, GOY);
				
				double oldX = GOX;
				double oldY = GOY;
				double newX, newY;
				
				c.LineWidth = 1.5;
				c.Color = Blue;
				foreach (MemStamp ms in StampBook) {
					if (ms.Type != null)
						continue;
				
					newX = GOX + (double)(ms.Seconds + ms.USeconds / 1000 - LowTime) / xscale;
					newY = GOY - (double)(ms.Bytes - LowBytes) / yscale;
					
					c.MoveTo (oldX, oldY);
					c.LineTo (newX, newY);
					c.Stroke ();
					
					oldX = newX;
					oldY = newY;
				}
				
				// Labels
				c.Color = Black;
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

				//Border
				c.Color = Black;
				c.LineWidth = 2;
				c.Rectangle (GOX, GOY, GW, -GH);
				c.Stroke ();
		}
		
		protected void ButtonPress (object o, ButtonPressEventArgs e)
		{
			Scale = 2;
			Draw ();
			QueueDrawArea (x, y, w, h);
			Console.Write ("Down : ");
		}
		
		protected void ButtonRelease (object o, ButtonReleaseEventArgs e)
		{
			Scale = 1;
			Draw ();
			QueueDrawArea (x, y, w, h);
			Console.WriteLine ("Up");
		}
	
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			myWindow = args.Window;
			
			myContext = Gdk.Context.CreateDrawable (myWindow);
			Draw ();
			
			return true;	
		}
	
	}
	
}
