//
// HeapResize.cs
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

	public class Resize {
		
		// The GC Generation during which the resize happened
		public int Generation;

		private long time_t;
		public DateTime Timestamp;

		public long PreviousSize;

		public long NewSize;

		public long TotalLiveBytes;

		public double PreResizeCapacity {
			get { return PreviousSize == 0 ? 0 : 100.0 * TotalLiveBytes / PreviousSize; }
		}

		public double PostResizeCapacity {
			get { return PreviousSize == 0 ? 0 : 100.0 * TotalLiveBytes / NewSize; }
		}


		// You need to set PreviousSize by hand.
		public void Read (BinaryReader reader, int generation)
		{
			if (generation < 0)
				Generation = reader.ReadInt32 ();
			else
				Generation = generation;
			time_t = reader.ReadInt64 ();
			Timestamp = Util.ConvertTimeT (time_t);
			NewSize = reader.ReadInt64 ();
			TotalLiveBytes = reader.ReadInt64 ();
		}

		public void Write (BinaryWriter writer)
		{
			writer.Write (Generation);
			writer.Write (time_t);
			writer.Write (NewSize);
			writer.Write (TotalLiveBytes);
		}
	}
}
