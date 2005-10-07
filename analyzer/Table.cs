//
// Table.cs
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
using System.Text;

namespace HeapBuddy {

	public enum Alignment {
		Right,
		Left,
		Center
	}

	public delegate string Stringify (object obj);

	public class Table {

		private int cols = -1;
		private string [] headers;
		private Alignment [] alignment;
		private int [] max_length;
		private Stringify [] stringify;
		private ArrayList rows = new ArrayList ();

		public int MaxRows = int.MaxValue;

		public int RowCount {
			get { return rows.Count; }
		}

		private void CheckColumns (ICollection whatever)
		{
			if (cols == -1) {

				cols = whatever.Count;
				if (cols == 0)
					throw new Exception ("Can't have zero columns!");

				alignment = new Alignment [cols];
				for (int i = 0; i < cols; ++i)
					alignment [i] = Alignment.Right;

				max_length = new int [cols];

				stringify = new Stringify [cols];
				
			} else if (cols != whatever.Count) {
				throw new Exception (String.Format ("Expected {0} columns, got {1}", cols, whatever.Count));
			}
		}
		
		public void AddHeaders (params string [] args)
		{
			CheckColumns (args);
			headers = args;
			for (int i = 0; i < cols; ++i) {
				int len = args [i].Length;
				if (len > max_length [i])
					max_length [i] = len;
			}
		}

		public void AddRow (params object [] row)
		{
			CheckColumns (row);
			rows.Add (row);
			for (int i = 0; i < cols; ++i) {
				string str;
				str = stringify [i] != null ? stringify [i] (row [i]) : row [i].ToString ();
				int len = str.Length;
				if (len > max_length [i])
					max_length [i] = len;
			}
		}

		public void SetAlignment (int i, Alignment align)
		{
			alignment [i] = align;
		}

		/////////////////////////////////////////////////////////////////////////////////////

		public void SetStringify (int i, Stringify s)
		{
			stringify [i] = s;
		}

		private class FormatClosure {
			
			string format;
			
			public FormatClosure (string format)
			{
				this.format = format;
			}

			public string Stringify (object obj)
			{
				return ((IFormattable) obj).ToString (format, null);
			}
		}

		public void SetStringify (int i, string format)
		{
			FormatClosure fc = new FormatClosure (format);
			SetStringify (i, new Stringify (fc.Stringify));
		}

		/////////////////////////////////////////////////////////////////////////////////////

		private class SortClosure : IComparer {

			int col;
			IComparer comparer;
			bool ascending;
			
			public SortClosure (int col, IComparer comparer, bool ascending)
			{
				this.col = col;
				this.comparer = comparer;
				this.ascending = ascending;
			}

			public int Compare (object x, object y)
			{
				int cmp;
				object [] row_x = (object []) x;
				object [] row_y = (object []) y;
				if (comparer == null)
					cmp = ((IComparable) row_x [col]).CompareTo (row_y [col]);
				else
					cmp = comparer.Compare (row_x [col], row_y [col]);
				if (! ascending)
					cmp = -cmp;
				return cmp;
			}
		}

		public void Sort (int i, IComparer comparer, bool ascending)
		{
			SortClosure sc;
			sc = new SortClosure (i, comparer, ascending);
			rows.Sort (sc);
		}

		public void Sort (int i)
		{
			Sort (i, null, true);
		}

		public void Sort (int i, bool ascending)
		{
			Sort (i, null, ascending);
		}

		/////////////////////////////////////////////////////////////////////////////////////

		private string GetColumnSeparator (int i)
		{
			if (0 <= i && i < cols-1)
				return " ";
			return "";
		}

		private string Pad (int length, Alignment alignment, string str)
		{
			switch (alignment) {
			case Alignment.Right:
				str = str.PadLeft (length);
				break;
			case Alignment.Left:
				str = str.PadRight (length);
				break;
			case Alignment.Center:
				str = str.PadLeft ((length + str.Length+1)/2).PadRight (length);
				break;
			}
			return str;
		}

		override public string ToString ()
		{
			StringBuilder sb;
			sb = new StringBuilder ();

			if (headers != null) {
				sb.Append (GetColumnSeparator (-1));
				for (int i = 0; i < cols; ++i) {
					sb.Append (Pad (max_length [i], Alignment.Center, headers [i]));
					sb.Append (GetColumnSeparator (i));
				}
				sb.Append ('\n');
			}

			int count = 0;
			foreach (object [] row in rows) {
				if (count != 0)
					sb.Append ('\n');
				sb.Append (GetColumnSeparator (-1));
				for (int i = 0; i < cols; ++i) {
					string str;
					str = stringify [i] != null ? stringify [i] (row [i]) : row [i].ToString ();
					str = Pad (max_length [i], alignment [i], str);
					sb.Append (str);
					sb.Append (GetColumnSeparator (i));
				}
				++count;
				if (count >= MaxRows)
					break;
			}

			return sb.ToString ();
		}
	}
}
