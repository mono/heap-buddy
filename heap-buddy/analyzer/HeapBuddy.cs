//
// HeapBuddy.cs
//
// Copyright (C) 2005 Novell, Inc.
//
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
