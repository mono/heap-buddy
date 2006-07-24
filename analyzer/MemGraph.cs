using System;
using System.Collections;
using Cairo;

namespace HeapBuddy {
		
	public class Memgraph {
	
		public class AllocData {
			public uint bytes;
			
			public AllocData () {
				bytes = 0;
			}
		}
	
		public Memgraph (OutfileReader reader, string data)
		{

			DisplayRawData (reader, data);
			
		}
		
		public void DisplayRawData (OutfileReader reader, string data)
		{
			int count = 0;
			Table table = new Table ();
			table.AddHeaders ("Time", "Allocated Bytes");
			
			Hashtable Data = new Hashtable ();
			AllocData ad;
						
			foreach (Backtrace bt in reader.Backtraces) {
				count++;
				if (data != null || bt.Type.Name == data) {
					if (Data.Contains (bt.TimeT))
						ad = (AllocData)Data[bt.TimeT];
					else {
						ad = new AllocData ();
						Data[bt.TimeT] = ad;
					}
						
					ad.bytes += bt.LastObjectStats.AllocatedTotalBytes;
				}
			}
			
			uint maxbytes = 0;
			uint minbytes = 100000000;
			uint avgbytes = 0;
						
			foreach (DictionaryEntry de in Data) {
				uint b = ((AllocData)de.Value).bytes;
			
				table.AddRow (de.Key, b);
				
				avgbytes += b;
				
				if (b < minbytes)
					minbytes = b;
				else if (b > maxbytes)
					maxbytes = b;
			}
			
			Console.WriteLine (table);			
			Console.WriteLine ("{0} allocations", count);
			Console.WriteLine ("Maximum Allocation: {0}", Util.PrettySize (maxbytes));
			Console.WriteLine ("Minimum Allocation: {0}", Util.PrettySize (minbytes));
			Console.WriteLine ("Average Allocation: {0}", Util.PrettySize (avgbytes / Data.Count));
		}
	}
}
