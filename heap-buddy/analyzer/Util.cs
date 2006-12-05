//
// Util.cs
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

namespace HeapBuddy {

	static public class Util {
		
		static DateTime base_time = new DateTime (1970, 1, 1, 0, 0, 0);
		static public DateTime ConvertTimeT (long time_t)
		{
			return base_time.AddSeconds (time_t);
		}

		/////////////////////////////////////////////////////////////////////////////////////

		static public bool ContainsNoCase (string haystack, string needle)
		{
			// FIXME: This could be much more efficient
			return haystack.ToLower ().IndexOf (needle.ToLower ()) >= 0;
		}

		/////////////////////////////////////////////////////////////////////////////////////

		static public string Ellipsize (int max_length, string str)
		{
			if (str.Length < max_length || max_length < 0)
				return str;
			
			return str.Substring (0, max_length/2 - 2) + "..." + str.Substring (str.Length - max_length/2 + 2);
		}

		static public string Ellipsize (object str)
		{
			return Ellipsize (40, (string) str);
		}

		/////////////////////////////////////////////////////////////////////////////////////

		static public string PrettySize (uint num_bytes)
		{
			if (num_bytes < 1024)
				return String.Format ("{0}b", num_bytes);

			if (num_bytes < 1024*10)
				return String.Format ("{0:0.0}k", num_bytes / 1024.0);

			if (num_bytes < 1024*1024)
				return String.Format ("{0}k", num_bytes / 1024);

			return String.Format ("{0:0.0}M", num_bytes / (1024 * 1024.0));
		}

		static public string PrettySize (long num_bytes)
		{
			return PrettySize ((uint) num_bytes);
		}

		static public string PrettySize_Obj (object obj)
		{
			return PrettySize ((uint) obj);
		}

		static public Stringify PrettySize_Stringify = new Stringify (PrettySize_Obj);
	}
}
