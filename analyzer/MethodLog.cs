using System;

namespace HeapBuddy {

	public class MethodLog {
	
		public MemZone Methods;
		public ulong   TotalBytes = 0;
		public uint    Count = 0;
		
		public MethodLog (OutfileReader reader) {
			Methods = new MemZone ();
			MemZone zone;
			
			// Build the data
			foreach (Backtrace bt in reader.Backtraces) {
				if (bt.Frames.Length <= 0)
					continue;
			
				Frame [] frames = (Frame [])bt.Frames.Clone ();
				Array.Reverse (frames);
				string name = frames[0].MethodName;
			
				if (Methods.Contains (name)) {
					zone = Methods[name];
				} else {
					zone = Methods[name] = new MemZone (name);
				}

				AddBacktrace (bt, zone);
				
				TotalBytes += zone.Bytes;
				Count++;
			}
			
			// Sort the data
			Methods.Sort ();
		}

		public void AddBacktrace (Backtrace bt, MemZone mz)
		{
			// Make sure the calls are in the data structures
			MergeBacktrace (bt, mz);
			
			// Add the new values to the calls
			UpdateBacktrace (bt, mz);
		}
		
		/*
		 * Make sure that all the methods in the
		 * call trace exist
		 */
		public void MergeBacktrace (Backtrace bt, MemZone mz)
		{
			MemZone Children = mz;
			
			Frame [] frames = (Frame [])bt.Frames.Clone ();
			Array.Reverse (frames);
		
			foreach (Frame f in frames) {
//				if (f.MethodName.StartsWith ("(wrapper"))
//					continue;
			
				if (!Children.Contains (f.MethodName))
					Children[f.MethodName] = new MemZone (f.MethodName);
								
				Children = Children[f.MethodName];
			}
		}
			
		/*
		 * Add in the number of allocations and the number
		 * of bytes in each method of this calltrace in the
		 * backtrace data structure.
		 *
		 * Errrr... something like that
		 */
		public void UpdateBacktrace (Backtrace bt, MemZone mz)
		{
			MemZone Children = mz;
			
			Frame [] frames = (Frame [])bt.Frames.Clone ();
			Array.Reverse (frames);
		
			foreach (Frame f in frames) {
				if (f.MethodName.IndexOf ("Main") != -1)
			
//				if (f.MethodName.StartsWith ("(wrapper"))
//					continue;
				
				Children[f.MethodName].Allocations++;
				Children.Bytes += bt.LastObjectStats.AllocatedTotalBytes;
				
				Children = Children[f.MethodName];
			}	
		}
	}
}
