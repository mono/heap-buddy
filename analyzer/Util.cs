//
// Util.cs
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

		static public string PrettyTime (long seconds)
		{
			if (seconds < 60)
				return String.Format ("{0}s", seconds);
			
			if (seconds < 3600)
				return String.Format ("{0:0.0}m", (float)seconds / 60.0);
			
			if (seconds < 3600 * 24)
				return String.Format ("{0:0.0}h", (float)seconds / 3600.0);
			
			return String.Format ("{0:0.0}d", (float)seconds / (3600.0 * 24));		
		}

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
