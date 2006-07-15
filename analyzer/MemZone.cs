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
