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
		public uint BacktraceCode;
		public ObjectStats ObjectStats;
	}

	public class Gc {

		private int generation;
		private long time_t;
		private DateTime timestamp;
		private long pre_gc_live_bytes;
		private long post_gc_live_bytes;
		private GcData [] gc_data;

		public int Generation {
			get { return generation; }
		}

		public DateTime Timestamp {
			get { return timestamp; }
		}

		public long PreGcLiveBytes {
			get { return pre_gc_live_bytes; }
		}

		public long PostGcLiveBytes {
			get { return post_gc_live_bytes; }
		}

		public long FreedBytes {
			get { return pre_gc_live_bytes - post_gc_live_bytes; }
		}

		public double FreedPercentage {
			get { return 100.0 * FreedBytes / pre_gc_live_bytes; }
		}

		public GcData [] GcData {
			get { return gc_data; }
		}

		/////////////////////////////////////////////////////////////////
		
		private void ReadHead (BinaryReader reader)
		{
			generation = reader.ReadInt32 ();
			time_t = reader.ReadInt64 ();
			timestamp = Util.ConvertTimeT (time_t);
			pre_gc_live_bytes = reader.ReadInt64 ();
		}

		public void ReadOnlyData (BinaryReader reader)
		{
			int n;
			n = reader.ReadInt32 ();

			gc_data = new GcData [n];
			for (int i = 0; i < n; ++i) {
				gc_data [i].BacktraceCode = reader.ReadUInt32 ();
				gc_data [i].ObjectStats.Read (reader);
			}
		}

		public void ReadWithData (BinaryReader reader)
		{
			ReadHead (reader);
			ReadOnlyData (reader);
			post_gc_live_bytes = reader.ReadInt64 ();
		}

		public void ReadWithoutData (BinaryReader reader)
		{
			ReadHead (reader);
			post_gc_live_bytes = reader.ReadInt64 ();
		}

		public void WriteWithoutData (BinaryWriter writer)
		{
			writer.Write (generation);
			writer.Write (time_t);
			writer.Write (pre_gc_live_bytes);
			writer.Write (post_gc_live_bytes);
		}

		public void WriteOnlyData (BinaryWriter writer)
		{
			combsort_gc_data ();
			writer.Write (gc_data.Length);
			for (int i = 0; i < gc_data.Length; ++i) {
				writer.Write (gc_data [i].BacktraceCode);
				gc_data [i].ObjectStats.Write (writer);
			}
		}

		/////////////////////////////////////////////////////////////////

		// This is copied from mono 1.1.8.3's implementation of System.Array

		static int new_gap (int gap)
                {
                        gap = (gap * 10) / 13;
                        if (gap == 9 || gap == 10)
                                return 11;
                        if (gap < 1)
                                return 1;
                        return gap;
                }

		void combsort_gc_data ()
                {
			int start = 0;
			int size = gc_data.Length;
                        int gap = size;
                        while (true) {
				gap = new_gap (gap);

                                bool swapped = false;
                                int end = start + size - gap;
                                for (int i = start; i < end; i++) {
                                        int j = i + gap;
                                        if (gc_data [i].BacktraceCode > gc_data [j].BacktraceCode) {
						GcData tmp;
						tmp = gc_data [i];
						gc_data [i] = gc_data [j];
						gc_data [j] = tmp;

                                                swapped = true;
                                        }
                                }
                                if (gap == 1 && !swapped)
                                        break;
                        }
		}

	}
}
