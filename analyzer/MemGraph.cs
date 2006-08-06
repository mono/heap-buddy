using System;
using System.Collections;
using Cairo;

namespace HeapBuddy {

	public class LongStats {
		private ArrayList Data;
		private long      Total;
		private bool      isSorted;
		
		public LongStats() {
			Data     = new ArrayList ();
			Total    = 0;
			isSorted = 0;
		}
		
		public void Add (long n) {
			Data.Add (n);
			isSorted = false;
			Total += n;
		}
		
		public long Min {
			get {
			
				if (Data.Count <= 0)
					throw new ArgumentException ();
				
				if (!isSorted) {
					Data.Sort ();
					isSorted = true;
				}
				
				return Data [0];
			}
		}
		
		public long Max {
			get {
				
				if (Data.Count <= 0)
					throw new ArgumentException ();
					
				if	(!isSorted) {
					Data.Sort ();
					isSorted = true;
				}
				
				return Data [Data.Count - 1];
			}		
		}
		
		public long Mean {
			get {
			
				if (Data.Count <= 0)
					throw new ArgumentException ();
				
				return (long)((float)Total / (float)Data.Count);
			}
		}
		
	}
		
	public class Memgraph {
		private LongStats Stats;
	
		public Memgraph (OutfileReader reader, string data)
		{
			Stats = new LongStats ();
			
			CollectStats (reader, data);			
			DisplayStats (Stats);
			
			ImageSurface surface = new ImageSurface (Format.RGB24, 320, 240);
			Context c = new Context (surface);
			
			c.Color = new Color (1, 1, 1, 1);
			c.Paint ();
			
			c.Color = new Color (0, 0, 0, 1);
			c.MoveTo (0, 0);
			c.LineTo (320, 240);
			c.LineWidth = 4;
			c.Stroke ();
			
			surface.WriteToPng ("memgraph.png");
			surface.Finish ();
		}
		
		public void CollectStats (OutfileReader reader, string data)
		{
			foreach (Gc gc in reader.Gcs) {
				Stats.Add (gc.PostGcLiveBytes);
			}
		}
		
		public void DisplayStats (LongStats stats)
		{
			try {
				Console.WriteLine (" Mix: {0}\n Max: {1}\nMean: {2}", stats.Min, stats.Max, stats.Mean);
			} catch { };
		}
}
