//
// Backtrace.cs
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

	public class Backtrace {

		public Type Type;
		
		public int LastGeneration;

		public ObjectStats LastObjectStats;

		public Frame [] frames;

		uint code;
		OutfileReader reader;

		public Backtrace (uint code, OutfileReader reader)
		{
			this.code = code;
			this.reader = reader;
		}

		public uint Code {
			get { return code; }
			set { code = value; }
		}

		public Frame [] Frames {
			
			get {
				if (frames == null)
					frames = reader.GetFrames (code);
				return frames;
			}

			set {
				frames = value;
			}
		}

		public bool MatchesType (string pattern)
		{
			return Type.Matches (pattern);
		}

		public bool MatchesMethod (string pattern)
		{
			int n = Frames.Length;
			for (int i = 0; i < n; ++i)
				if (Util.ContainsNoCase (frames [i].MethodName, pattern))
					return true;
			return false;
		}

		public bool Matches (string pattern)
		{
			return MatchesType (pattern) || MatchesMethod (pattern);
		}
	}

}
