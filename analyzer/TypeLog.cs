
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

namespace HeapBuddy {

	public class TypeLog {
	
		public MemZone Types;
		public ulong   TotalBytes = 0;
		public uint    Count = 0;
		
		public TypeLog (OutfileReader reader) {
			Types = new MemZone ();
			MemZone zone;
			
			// Build the data
			foreach (Backtrace bt in reader.Backtraces) {
				if (Types.Contains (bt.Type.Name)) {
					zone = Types[bt.Type.Name];
				} else {
					zone = Types[bt.Type.Name] = new MemZone (bt.Type.Name);
				}

				AddBacktrace (bt, zone);
				
				TotalBytes += zone.Bytes;
				Count++;
			}
			
			// Sort the data
			Types.Sort ();
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
			MemZone Callers = mz;
		
			foreach (Frame f in bt.Frames) {
//				if (f.MethodName.StartsWith ("(wrapper"))
//					continue;
			
				if (!Callers.Contains (f.MethodName))
					Callers[f.MethodName] = new MemZone (f.MethodName);
								
				Callers = Callers[f.MethodName];
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
			MemZone Callers = mz;
			
			mz.Allocations    += bt.LastObjectStats.AllocatedCount;
			mz.Bytes          += bt.LastObjectStats.AllocatedTotalBytes;
			
			Types.Allocations += bt.LastObjectStats.AllocatedCount;
			Types.Bytes       += bt.LastObjectStats.AllocatedTotalBytes;
		
			foreach (Frame f in bt.Frames) {
//				if (f.MethodName.StartsWith ("(wrapper"))
//					continue;
				
				Callers[f.MethodName].Allocations += bt.LastObjectStats.AllocatedCount;
				Callers[f.MethodName].Bytes       += bt.LastObjectStats.AllocatedTotalBytes;
				
				Callers = Callers[f.MethodName];
			}	
		}
	}
}
