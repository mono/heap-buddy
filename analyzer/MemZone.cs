
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
using System.Collections;

namespace HeapBuddy {

	public class MemZone {
		public uint      Allocations;
		public uint      Bytes;
		public Hashtable MethodHash;
		public ArrayList Methods;
		public string    Name;		

		public MemZone ()
		{
			MethodHash  = new Hashtable ();
			Methods     = new ArrayList ();
			Allocations = 0;
			Bytes       = 0;
		}
		
		public MemZone (string n)
		{
			MethodHash  = new Hashtable ();
			Methods     = new ArrayList ();
			Allocations = 0;
			Bytes       = 0;
			Name        = n;
		}
		
		public bool Contains (string n) {
			return MethodHash.ContainsKey (n);
		}
		
		/*
		 * Return null if name not found
		 *
		 * Throws ArgumentException if
		 * MethodHash already contains n
		 */
		public MemZone this[string n] {
			get {
				if (n == null || n == "")
					throw new ArgumentException ();
			
				return (MemZone)MethodHash[n];
			}
			
			set {
				if (MethodHash.ContainsKey (n))
					throw new ArgumentException ();

				Methods.Add (value);
				MethodHash.Add (n, value);
			}
		}
		
		public MemZone this[int n] {
			get {
				if (n < 0 || n >= Methods.Count)
					return null;
				
				return (MemZone)Methods[n];
			}
		}
		
		public void Sort () {
			IComparer ic = new MemZoneComparer ();
			
			foreach (MemZone mz in Methods)
				mz.Sort ();
			
			Methods.Sort (ic);
		}
		
	}
	
	public class MemZoneComparer : IComparer {
	
		int IComparer.Compare (Object x, Object y) {
			MemZone a = (MemZone)x;
			MemZone b = (MemZone)y;
		
			if (a.Bytes > b.Bytes) return -1;
			else if (a.Bytes < b.Bytes) return 1;
			else return 0;
		}
		
	}
	
}
