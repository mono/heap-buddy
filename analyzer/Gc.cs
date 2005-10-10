//
// Gc.cs
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
using System.IO;

namespace HeapBuddy {

	public struct GcData {
		public Backtrace Backtrace;
		public ObjectStats ObjectStats;
	}

	public class Gc {

		public int Generation;

		public long TimeT;
		public DateTime Timestamp;

		public long PreGcLiveBytes;
		public int  PreGcLiveObjects;
		public long PostGcLiveBytes;
		public int  PostGcLiveObjects;

		private GcData [] gc_data;
		OutfileReader reader;

		/////////////////////////////////////////////////////////////////

		public Gc (OutfileReader reader)
		{
			this.reader = reader;
		}

		/////////////////////////////////////////////////////////////////

		public long FreedBytes {
			get { return PreGcLiveBytes - PostGcLiveBytes; }
		}

		public int FreedObjects {
			get { return PreGcLiveObjects - PostGcLiveObjects; }
		}

		public double FreedBytesPercentage {
			get { return PreGcLiveBytes == 0 ? 0 : 100.0 * FreedBytes / PreGcLiveBytes; }
		}

		public double FreedObjectsPercentage {
			get { return PreGcLiveObjects == 0 ? 0 : 100.0 * FreedObjects / PreGcLiveObjects; }
		}

		public GcData [] GcData {
			get { 
				if (gc_data == null)
					gc_data = reader.GetGcData (Generation);
				return gc_data; 
			}
			
			set { gc_data = value; }
		}
	}
}
