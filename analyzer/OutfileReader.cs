//
// OutfileReader.cs
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
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace HeapBuddy {

	public class OutfileReader {

		public bool Debug = false;

		const uint magic_number = 0x4eabbdd1;
		const int expected_log_version = 4;
		const int expected_summary_version = 1;
		const string log_file_label = "heap-buddy logfile";
		const string summary_file_label = "heap-buddy summary";

		//
		// Data from the outfile's header
		//

		bool terminated_normally;

		// Object counts
		int n_gcs;
		int n_types;
		int n_methods;
		int n_backtraces;
		int n_resizes;

		// Offsets in the summary file
		long type_name_data_offset = -1;
		long method_name_data_offset = -1;
		long backtrace_data_offset = -1;
		long gc_data_offset = -1;
		long resize_data_offset = -1;
		long types_by_code_offset = -1;
		long methods_by_code_offset = -1;
		long backtrace_index_offset = -1;
		long gc_index_offset = -1;

		// The reader to use for lazy lookups of names, etc.
		BinaryReader lazy_reader = null;

		///////////////////////////////////////////////////////////////////

		private struct Method {
			public string Name;
			public long Position; // of the name in the summary file
		}

		private struct Frame {
			public uint MethodCode;
			public uint IlOffset;
		}

		private struct Backtrace {
			public uint TypeCode;
			public Frame [] Frames;
			public int LastGeneration;
			public ObjectStats LastObjectStats;
			public long Position;
		}

		Type [] types;
		Method [] methods;
		Backtrace [] backtraces;
		Gc [] gcs;
		long [] gc_pos;
		Resize [] resizes;

		// These are only needed for log files
		uint [] type_codes_old;
		uint [] type_codes_new;
		uint [] method_codes_old;
		uint [] method_codes_new;
		uint [] backtrace_codes;
		
		///////////////////////////////////////////////////////////////////

		public OutfileReader (string filename)
		{
			Stream stream;
			stream = new FileStream (filename, FileMode.Open, FileAccess.Read);

			BinaryReader reader;
			reader = new BinaryReader (stream);
			
			bool is_summary;
			is_summary = ReadPreamble (reader);

			Spew ("This is a {0} file", is_summary ? "summary" : "log");
			
			ReadHeader (reader);
			
			if (is_summary) {
				lazy_reader = reader;
				ReadSummaryFile (reader);
			} else {
				
				type_codes_old = new uint [n_types];
				type_codes_new = new uint [n_types];
				method_codes_old = new uint [n_methods];
				method_codes_new = new uint [n_methods];
				backtrace_codes = new uint [n_backtraces];

				ReadLogFile (reader);
				reader.Close ();

				RemapAllCodes ();

				CollectFinalBacktraceAndTypeStats ();

				string tmp_filename;
				tmp_filename = Path.GetTempFileName ();
				stream = new FileStream (tmp_filename, FileMode.Open, FileAccess.Write);
				
				BinaryWriter writer;
				writer = new BinaryWriter (stream);
				WriteSummaryFile (writer);
				writer.Close ();

				// Replace the log file with the summary file.
				File.Copy (tmp_filename, filename, true /* allow overwrite */);
				File.Delete (tmp_filename);

				// We don't need to set lazy_reader, since all
				// of the data will already be in memory.
			}
		}

		///////////////////////////////////////////////////////////////////

		private void Spew (string format, params object [] args)
		{
			if (Debug) {
				string message;
				message = String.Format (format, args);
				Console.WriteLine (message);
			}
		}

		///////////////////////////////////////////////////////////////////

		// Return true if this is a summary file, false if it is a log file.
		private bool ReadPreamble (BinaryReader reader)
		{
			uint this_magic;
			this_magic = reader.ReadUInt32 ();
			if (this_magic != magic_number)
				throw new Exception ("Bad magic number in heap-buddy outfile");

			int this_version;
			this_version = reader.ReadInt32 ();

			string this_label;
			bool is_summary;
			int expected_version;

			this_label = reader.ReadString ();
			if (this_label == log_file_label) {
				is_summary = false;
				expected_version = expected_log_version;
			} else if (this_label == summary_file_label) {
				is_summary = true;
				expected_version = expected_summary_version;
			} else
				throw new Exception ("Unknown file label in heap-buddy outfile");

			if (this_version != expected_version) {
				string msg;
				msg = String.Format ("Version error in {0}: expected {1}, found {2}",
						     this_label, expected_version, this_version);
				throw new Exception (msg);
			}

			return is_summary;
		}

		private void WritePreamble (BinaryWriter writer)
		{
			writer.Write (magic_number);
			writer.Write (expected_summary_version);
			writer.Write (summary_file_label); // we only write summary files from C#
		}

		///////////////////////////////////////////////////////////////////

		private void ReadHeader (BinaryReader reader)
		{
			Spew ("Reading header");

			byte termination_byte;
			termination_byte = reader.ReadByte ();
			if (termination_byte == 1)
				terminated_normally = true;
			else if (termination_byte != 0)
				throw new Exception ("Unexpected termination status byte: " + termination_byte);

			Spew ("Log is {0}", terminated_normally ? "complete" : "truncated");

			n_gcs = reader.ReadInt32 ();
			n_types = reader.ReadInt32 ();
			n_methods = reader.ReadInt32 ();
			n_backtraces = reader.ReadInt32 ();
			n_resizes = reader.ReadInt32 ();

			Spew ("GCs = {0}", n_gcs);
			Spew ("Types = {0}", n_types);
			Spew ("Methods = {0}", n_methods);
			Spew ("Backtraces = {0}", n_backtraces);
			Spew ("Resizes = {0}", n_resizes);

			types = new Type [n_types];
			methods = new Method [n_methods];
			backtraces = new Backtrace [n_backtraces];
			gcs = new Gc [n_gcs];
			gc_pos = new long [n_gcs];
			resizes = new Resize [n_resizes];

			Spew ("Finished reading header");
		}

		private void WriteHeader (BinaryWriter writer)
		{
			Spew ("Writing header");

			// We only write out summary files, which
			// are never truncated.
			writer.Write ((byte) 1);

			writer.Write (n_gcs);
			writer.Write (n_types);
			writer.Write (n_methods);
			writer.Write (n_backtraces);
			writer.Write (n_resizes);

			Spew ("Finished writing header");
		}

		///////////////////////////////////////////////////////////////////
		
		//
		// Code to read the log files generated at runtime
		//

		// These need to agree w/ the definitions in outfile-writer.c
		const byte TAG_TYPE      = 0x01;
		const byte TAG_METHOD    = 0x02;
		const byte TAG_BACKTRACE = 0x03;
		const byte TAG_GC        = 0x04;
		const byte TAG_RESIZE    = 0x05;
		const byte TAG_EOS       = 0xff;

		int i_type = 0, i_method = 0, i_backtrace = 0, i_gc = 0, i_resize = 0;

		private void ReadLogFile (BinaryReader reader)
		{
			int chunk_count = 0;

			try {
				while (ReadLogFileChunk (reader))
					++chunk_count;

			} catch (System.IO.EndOfStreamException) {
				// This means that the outfile was truncated.
				// In that case, just do nothing --- except if the file
				// claimed that things terminated normally.
				if (terminated_normally)
					throw new Exception ("The heap log did not contain TAG_EOS, "
							     + "but the outfile was marked as having been terminated normally, so "
							     + "something must be terribly wrong.");
			}
			Spew ("Processed {0} chunks", chunk_count);

			if (i_type != n_types)
				throw new Exception (String.Format ("Found {0} types, expected {1}", i_type, n_types));

			if (i_method != n_methods)
				throw new Exception (String.Format ("Found {0} methods, expected {1}", i_method, n_methods));

			if (i_backtrace != n_backtraces)
				throw new Exception (String.Format ("Found {0} backtraces, expected {1}", i_backtrace, n_backtraces));

			if (i_gc != n_gcs)
				throw new Exception (String.Format ("Found {0} GCs, expected {1}", i_gc, n_gcs));

			if (i_resize != n_resizes)
				throw new Exception (String.Format ("Found {0} resizes, expected {1}", i_resize, n_resizes));
		}

		private bool ReadLogFileChunk (BinaryReader reader)
		{

			// FIXME: This will fail on truncated outfiles

			byte tag = reader.ReadByte ();

			switch (tag) {
			case TAG_TYPE:
				ReadLogFileChunk_Type (reader);
				break;
					
			case TAG_METHOD:
				ReadLogFileChunk_Method (reader);
				break;
				
			case TAG_BACKTRACE:
				ReadLogFileChunk_Backtrace (reader);
				break;
				
			case TAG_GC:
				ReadLogFileChunk_Gc (reader);
				break;
				
			case TAG_RESIZE:
				ReadLogFileChunk_Resize (reader);
				break;
				
			case TAG_EOS:
				//Spew ("Found EOS");
				return false;

			default:
				throw new Exception ("Unknown tag! " + tag);
			}

			return true;
		}

		private void ReadLogFileChunk_Type (BinaryReader reader)
		{
			uint code;
			code = reader.ReadUInt32 ();

			string name;
			name = reader.ReadString ();

			if (i_type >= n_types)
				return;

			//Spew ("Found type '{0}'", name);

			type_codes_old [i_type] = code;

			Type type;
			type = new Type ();
			type.Name = name;
			types [i_type] = type;

			++i_type;
		}

		private void ReadLogFileChunk_Method (BinaryReader reader)
		{
			uint code;
			code = reader.ReadUInt32 ();

			string name;
			name = reader.ReadString ();

			if (i_method >= n_methods)
				return;

			//Spew ("Found method '{0}' with code {1}", name, code);

			method_codes_old [i_method] = code;
			methods [i_method].Name = name;

			++i_method;
		}

		private void ReadLogFileChunk_Backtrace (BinaryReader reader)
		{
			uint code;
			code = reader.ReadUInt32 ();
			
			uint type_code;
			type_code = reader.ReadUInt32 ();
			
			int n_frames;
			n_frames = reader.ReadInt16 ();

			if (i_backtrace >= n_backtraces) {
				for (int i = 0; i < n_frames; ++i) {
					reader.ReadUInt32 (); // skip method code
					reader.ReadUInt32 (); // skip native offset
				}
				return;
			}

			backtrace_codes [i_backtrace] = code;
			backtraces [i_backtrace].TypeCode = type_code;

			Frame [] frames = new Frame [n_frames];
			backtraces [i_backtrace].Frames = frames;

			for (int i = 0; i < n_frames; ++i) {
				frames [i].MethodCode = reader.ReadUInt32 ();
				frames [i].IlOffset = reader.ReadUInt32 ();
			}

			++i_backtrace;
		}

		private void ReadLogFileChunk_Gc (BinaryReader reader)
		{
			Gc gc;
			gc = new Gc ();
			gc.ReadWithData (reader);

			gcs [i_gc] = gc;
			++i_gc;

			if (gc.Generation >= 0)
				Spew ("GC {0}: collected {1} bytes, {2} to {3}",
				      gc.Generation,
				      gc.FreedBytes,
				      gc.PreGcLiveBytes,
				      gc.PostGcLiveBytes);
		}

		private void ReadLogFileChunk_Resize (BinaryReader reader)
		{
			Resize r;
			r = new Resize ();
			r.Read (reader, i_gc);
			if (i_resize > 0)
				r.PreviousSize = resizes [i_resize-1].NewSize;
			Spew ("Resize to {0}, {1} live bytes", r.NewSize, r.TotalLiveBytes);
			resizes [i_resize] = r;
			++i_resize;
		}

		///////////////////////////////////////////////////////////////////

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

		private enum SortOrder {
			ByCode,
			ByName
		}

		void combsort_types (SortOrder order)
                {
			int start = 0;
			int size = types.Length;
                        int gap = size;
                        while (true) {
				gap = new_gap (gap);

                                bool swapped = false;
                                int end = start + size - gap;
                                for (int i = start; i < end; i++) {
                                        int j = i + gap;

					bool out_of_order;
					if (order == SortOrder.ByCode)
						out_of_order = type_codes_old [i] > type_codes_old [j];
					else
						out_of_order = String.Compare (types [i].Name, types [j].Name) > 0;
						
                                        if (out_of_order) {

						uint tmp_code;
						Type tmp;

						tmp_code = type_codes_old [i];
						type_codes_old [i] = type_codes_old [j];
						type_codes_old [j] = tmp_code;

						tmp_code = type_codes_new [i];
						type_codes_new [i] = type_codes_new [j];
						type_codes_new [j] = tmp_code;

						tmp = types [i];
						types [i] = types [j];
						types [j] = tmp;

						swapped = true;
                                        }
                                }
                                if (gap == 1 && !swapped)
                                        break;
                        }
                }

		void combsort_methods (SortOrder order)
                {
			int start = 0;
			int size = methods.Length;
                        int gap = size;
                        while (true) {
				gap = new_gap (gap);

                                bool swapped = false;
                                int end = start + size - gap;
                                for (int i = start; i < end; i++) {
                                        int j = i + gap;

					bool out_of_order;
					if (order == SortOrder.ByCode)
						out_of_order = method_codes_old [i] > method_codes_old [j];
					else
						out_of_order = String.Compare (methods [i].Name, methods [j].Name) > 0;

                                        if (out_of_order) {

						uint tmp_code;
						Method tmp;

						tmp_code = method_codes_old [i];
						method_codes_old [i] = method_codes_old [j];
						method_codes_old [j] = tmp_code;

						tmp_code = method_codes_new [i];
						method_codes_new [i] = method_codes_new [j];
						method_codes_new [j] = tmp_code;

						tmp = methods [i];
						methods [i] = methods [j];
						methods [j] = tmp;

                                                swapped = true;
                                        }
                                }
                                if (gap == 1 && !swapped)
                                        break;
                        }
                }

		void combsort_backtraces ()
                {
			int start = 0;
			int size = backtraces.Length;
                        int gap = size;
                        while (true) {
				gap = new_gap (gap);

                                bool swapped = false;
                                int end = start + size - gap;
                                for (int i = start; i < end; i++) {
                                        int j = i + gap;
                                        if (backtrace_codes [i] > backtrace_codes [j]) {

						uint tmp_code;
						tmp_code = backtrace_codes [i];
						backtrace_codes [i] = backtrace_codes [j];
						backtrace_codes [j] = tmp_code;

						Backtrace tmp;
						tmp = backtraces [i];
						backtraces [i] = backtraces [j];
						backtraces [j] = tmp;

                                                swapped = true;
                                        }
                                }
                                if (gap == 1 && !swapped)
                                        break;
                        }
		}

		///////////////////////////////////////////////////////////////////

		private uint TranslateTypeCode (uint code)
		{
			int i, i0, i1;
			i0 = 0;
			i1 = types.Length-1;
			
			while (i0 <= i1) {
				i = (i0 + i1) / 2;
				if (type_codes_old [i] == code)
					return type_codes_new [i];
				else if (type_codes_old [i] < code)
					i0 = i+1;
				else
					i1 = i-1;
			}

			throw new Exception ("Couldn't resolve type code " + code);
		}

		private uint TranslateMethodCode (uint code)
		{
			int i, i0, i1;
			i0 = 0;
			i1 = methods.Length-1;
			
			while (i0 <= i1) {
				i = (i0 + i1) / 2;
				if (method_codes_old [i] == code)
					return method_codes_new [i];
				else if (method_codes_old [i] < code)
					i0 = i+1;
				else
					i1 = i-1;
			}

			throw new Exception ("Couldn't resolve method code " + code);
		}

		private uint TranslateBacktraceCode (uint code)
		{
			int i, i0, i1;
			i0 = 0;
			i1 = backtraces.Length-1;
			
			while (i0 <= i1) {
				i = (i0 + i1) / 2;
				if (backtrace_codes [i] == code)
					return (uint) i;
				else if (backtrace_codes [i] < code)
					i0 = i+1;
				else
					i1 = i-1;
			}

			throw new Exception ("Couldn't resolve backtrace code " + code);
		}

		private void RemapAllCodes ()
		{
			combsort_types (SortOrder.ByName);
			for (int i = 0; i < type_codes_new.Length; ++i)
				type_codes_new [i] = (uint) i;
			combsort_types (SortOrder.ByCode); // this sorts by the old codes
			
			combsort_methods (SortOrder.ByName);
			for (int i = 0; i < method_codes_new.Length; ++i)
				method_codes_new [i] = (uint) i;
			combsort_methods (SortOrder.ByCode); // again, this sorts by the old codes

			combsort_backtraces ();

			// Remap the backtrace codes in the GCs
			for (int i = 0; i < gcs.Length; ++i) {
				for (int j = 0; j < gcs [i].GcData.Length; ++j) {
					uint code;
					code = gcs [i].GcData [j].BacktraceCode;
					code = TranslateBacktraceCode (code);
					gcs [i].GcData [j].BacktraceCode = code;
				}
			}

			// Remap the type and method codes in the backtrace,
			// and replace the backtrace codes.
			for (int i = 0; i < backtraces.Length; ++i) {
				backtraces [i].TypeCode = TranslateTypeCode (backtraces [i].TypeCode);
				for (int j = 0; j < backtraces [i].Frames.Length; ++j) {
					uint code;
					code = backtraces [i].Frames [j].MethodCode;
					code = TranslateMethodCode (code);
					backtraces [i].Frames [j].MethodCode = code;
				}
			}

			// Re-sort them back into name order, which is the same as sorting by the new
			// codes.  This puts everything into the correct order for when we write
			// them out to the summary file.
			combsort_types (SortOrder.ByName);
			combsort_methods (SortOrder.ByName);

			// After remapping the codes, we don't need these any more.
			type_codes_old = null;
			type_codes_new = null;
			method_codes_old = null;
			method_codes_new = null;
			backtrace_codes = null;
		}

		///////////////////////////////////////////////////////////////////

		private void CollectFinalBacktraceAndTypeStats ()
		{
			for (int i = 0; i < backtraces.Length; ++i)
				backtraces [i].LastGeneration = int.MaxValue;

			for (int i = 0; i < types.Length; ++i)
				types [i].LastGeneration = int.MaxValue;

			int count;
			count = backtraces.Length;

			for (int i = gcs.Length - 1; i >= 0; --i) {
				for (int j = 0; j < gcs [i].GcData.Length; ++j) {
					uint bt_code;
					bt_code = gcs [i].GcData [j].BacktraceCode;
					if (backtraces [bt_code].LastGeneration == int.MaxValue) {
						backtraces [bt_code].LastGeneration = gcs [i].Generation;
						backtraces [bt_code].LastObjectStats = gcs [i].GcData [j].ObjectStats;
						--count;

						// Add this backtrace to our per-type totals
						uint type_code;
						type_code = backtraces [bt_code].TypeCode;
						types [type_code].BacktraceCount++;
						if (types [type_code].LastGeneration == int.MaxValue) {
							types [type_code].LastGeneration = backtraces [bt_code].LastGeneration;
							types [type_code].LastObjectStats = backtraces [bt_code].LastObjectStats;
						} else if (types [type_code].LastGeneration == backtraces [bt_code].LastGeneration) {
							types [type_code].LastObjectStats += backtraces [bt_code].LastObjectStats;
						} else {
							types [type_code].LastObjectStats.AddAllocatedOnly (backtraces [bt_code].LastObjectStats);
						}
					}
				}

				// If we've found stats for every backtrace, bail out of the loop early.
				if (count == 0)
					break;
			}
		}

		///////////////////////////////////////////////////////////////////

		private void ReadSummaryTableOfContents (BinaryReader reader)
		{
			type_name_data_offset = reader.ReadInt64 ();
			method_name_data_offset = reader.ReadInt64 ();
			backtrace_data_offset = reader.ReadInt64 ();
			gc_data_offset = reader.ReadInt64 ();
			resize_data_offset = reader.ReadInt64 ();
			types_by_code_offset = reader.ReadInt64 ();
			methods_by_code_offset = reader.ReadInt64 ();
			backtrace_index_offset = reader.ReadInt64 ();
			gc_index_offset = reader.ReadInt64 ();
		}

		private void WriteSummaryTableOfContents (BinaryWriter writer)
		{
			writer.Write (type_name_data_offset);
			writer.Write (method_name_data_offset);
			writer.Write (backtrace_data_offset);
			writer.Write (gc_data_offset);
			writer.Write (resize_data_offset);
			writer.Write (types_by_code_offset);
			writer.Write (methods_by_code_offset);
			writer.Write (backtrace_index_offset);
			writer.Write (gc_index_offset);
		}

		///////////////////////////////////////////////////////////////////

		//
		// Summary file reader
		//

		private void ReadSummaryFile (BinaryReader reader)
		{
			ReadSummaryTableOfContents (reader);
			ReadSummaryIndexes (reader);
			ReadSummaryTypes (reader);
			ReadSummaryResizes (reader);
			ReadSummaryGcs (reader);
		}

		private void ReadSummaryIndexes (BinaryReader reader)
		{
			reader.BaseStream.Seek (methods_by_code_offset, SeekOrigin.Begin);
			for (int i = 0; i < methods.Length; ++i) 
				methods [i].Position = reader.ReadInt64 ();

			reader.BaseStream.Seek (backtrace_index_offset, SeekOrigin.Begin);
			for (int i = 0; i < backtraces.Length; ++i) {
				backtraces [i].TypeCode = reader.ReadUInt32 ();
				backtraces [i].LastGeneration = reader.ReadInt32 ();
				backtraces [i].LastObjectStats.Read (reader);
				backtraces [i].Position = reader.ReadInt64 ();
			}
		}

		private void ReadSummaryTypes (BinaryReader reader)
		{
			reader.BaseStream.Seek (type_name_data_offset, SeekOrigin.Begin);
			for (int i = 0; i < types.Length; ++i) {
				Type type;
				type = new Type ();
				type.Name = reader.ReadString ();
				types [i] = type;
			}

			reader.BaseStream.Seek (types_by_code_offset, SeekOrigin.Begin);
			for (int i = 0; i < types.Length; ++i) {
				Type type;
				type = types [i];
				type.BacktraceCount = reader.ReadInt32 ();
				type.LastGeneration = reader.ReadInt32 ();
				type.LastObjectStats.Read (reader);
			}
		}

		private void ReadSummaryResizes (BinaryReader reader)
		{
			reader.BaseStream.Seek (resize_data_offset, SeekOrigin.Begin);
			for (int i = 0; i < resizes.Length; ++i) {
				Resize r;
				r = new Resize ();
				r.Read (reader, -1);
				if (i > 0)
					r.PreviousSize = resizes [i-1].NewSize;
				resizes [i] = r;
			}
		}

		private void ReadSummaryGcs (BinaryReader reader)
		{
			reader.BaseStream.Seek (gc_index_offset, SeekOrigin.Begin);
			for (int i = 0; i < gcs.Length; ++i) {
				Gc gc;
				gc = new Gc ();
				gc.ReadWithoutData (reader);
				gcs [i] = gc;
				gc_pos [i] = reader.ReadInt64 ();
			}
		}

		///////////////////////////////////////////////////////////////////

		//
		// Summary file writer
		//

		private void WriteSummaryFile (BinaryWriter writer)
		{
			WritePreamble (writer);
			WriteHeader (writer);

			long toc_offset;
			toc_offset = writer.BaseStream.Position;
			WriteSummaryTableOfContents (writer); // writes placeholder data
			
			WriteSummaryData (writer);
			
			WriteSummaryIndexes (writer);

			WriteSummaryTypes (writer);

			writer.BaseStream.Seek (toc_offset, SeekOrigin.Begin);
			WriteSummaryTableOfContents (writer); // writes the actual data
		}

		private void WriteSummaryData (BinaryWriter writer)
		{
			// Write out the name strings.
			type_name_data_offset = writer.BaseStream.Position;
			for (int i = 0; i < types.Length; ++i)
				writer.Write (types [i].Name);


			// Write out the method names, and remember the position
			// of each in the file.
			method_name_data_offset = writer.BaseStream.Position;
			for (int i = 0; i < methods.Length; ++i) {
				methods [i].Position = writer.BaseStream.Position;
				writer.Write (methods [i].Name);
			}
			
			
			// Write out all of the backtrace frame data, and remember the position
			// of each in the file.
			backtrace_data_offset = writer.BaseStream.Position;
			for (int i = 0; i < backtraces.Length; ++i) {
				backtraces [i].Position = writer.BaseStream.Position;
				writer.Write (backtraces [i].Frames.Length);
				for (int j = 0; j < backtraces [i].Frames.Length; ++j) {
					writer.Write (backtraces [i].Frames [j].MethodCode);
					writer.Write (backtraces [i].Frames [j].IlOffset);
				}
			}

			
			// Write out all of the GC data, and remember the position of
			// each in the file.
			gc_data_offset = writer.BaseStream.Position;
			for (int i = 0; i < gcs.Length; ++i) {
				gc_pos [i] = writer.BaseStream.Position;
				gcs [i].WriteOnlyData (writer);
			}


			// Write out all the resizes.
			resize_data_offset = writer.BaseStream.Position;
			for (int i = 0; i < resizes.Length; ++i)
				resizes [i].Write (writer);
		}

		private void WriteSummaryIndexes (BinaryWriter writer)
		{
			methods_by_code_offset = writer.BaseStream.Position;
			for (int i = 0; i < methods.Length; ++i)
				writer.Write (methods [i].Position);

			// backtraces were sorted in WriteSummary
			backtrace_index_offset = writer.BaseStream.Position;
			for (int i = 0; i < backtraces.Length; ++i) {
				writer.Write (backtraces [i].TypeCode);
				writer.Write (backtraces [i].LastGeneration);
				backtraces [i].LastObjectStats.Write (writer);
				writer.Write (backtraces [i].Position);
			}

			gc_index_offset = writer.BaseStream.Position;
			for (int i = 0; i < gcs.Length; ++i) {
				gcs [i].WriteWithoutData (writer);
				writer.Write (gc_pos [i]);
			}
		}

		private void WriteSummaryTypes (BinaryWriter writer)
		{
			types_by_code_offset = writer.BaseStream.Position;
			for (int i = 0; i < types.Length; ++i) {
				Type type;
				type = types [i];
				writer.Write (type.BacktraceCount);
				writer.Write (type.LastGeneration);
				type.LastObjectStats.Write (writer);
			}
		}

		///////////////////////////////////////////////////////////////////

		public Resize [] Resizes {
			get { return resizes; }
		}

		public Gc [] Gcs {
			get { return gcs; }
		}

		///////////////////////////////////////////////////////////////////

		public Type [] Types {
			get { return types; }
		}

		public ArrayList GetTypesMatching (Regex regex)
		{
			ArrayList matches;
			matches = new ArrayList ();
			for (int i = 0; i < types.Length; ++i)
				if (regex.Match (types [i].Name).Success)
					matches.Add (types [i]);
			return matches;
		}

		public ArrayList GetTypesMatching (string regex)
		{
			return GetTypesMatching (new Regex (regex));
		}

		///////////////////////////////////////////////////////////////////

		public string GetMethodName (uint code)
		{
			if (methods [code].Name == null) {
				lazy_reader.BaseStream.Seek (methods [code].Position, SeekOrigin.Begin);
				methods [code].Name = lazy_reader.ReadString ();
			}

			return methods [code].Name;
		}

#if false
		private void PopulateBacktrace (uint code)
		{
			if (backtraces [code].Frames != null)
				return;

			lazy_reader.BaseStream.Seek (backtraces [code].Position, SeekOrigin.Begin);

			int length;
			length = lazy_reader.ReadInt32 ();

			Frame [] frames;
			frames = new Frame [length];
			for (int i = 0; i < length; ++i) {
				frames [i].MethodCode = lazy_reader.ReadUInt32 ();
				frames [i].IlOffset = lazy_reader.ReadUInt32 ();
			}

			backtraces [code].Frames = frames;
		}
#endif
	}
}
