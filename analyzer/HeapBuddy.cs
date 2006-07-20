//
// HeapBuddy.cs
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

namespace HeapBuddy {

	static public class HeapBuddyMain {

		static public void Main (string [] args)
		{
			int args_i = 0;

			string outfile_name = "outfile";
			if (args_i < args.Length && File.Exists (args [args_i])) {
				outfile_name = args [args_i];
				++args_i;
			}

			if (! File.Exists (outfile_name)) {
				Console.WriteLine ("Can't find outfile '{0}'", outfile_name);
				return;
			}

			string report_name = "summary";
			if (args_i < args.Length && Report.Exists (args [args_i])) {
				report_name = args [args_i];
				++args_i;
			}

			Report report;
			report = Report.Get (report_name);
			
			OutfileReader reader;
			reader = new OutfileReader (outfile_name);

			string [] remaining_args = new string [args.Length - args_i];
			for (int i = args_i; i < args.Length; ++i)
				remaining_args [i - args_i] = args [i];
			
			Console.WriteLine ();
			report.Run (reader, remaining_args);
			Console.WriteLine ();
		}
	}

}
