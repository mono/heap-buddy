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
