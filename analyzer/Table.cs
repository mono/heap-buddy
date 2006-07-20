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

		private int n_cols = -1;
		private string [] headers;
		private Alignment [] alignment;
		private Stringify [] stringify;
		private ArrayList rows = new ArrayList ();

		public int MaxRows = int.MaxValue;
		public bool SkipLines = false;
		public string Separator = " ";

		public int RowCount {
			get { return rows.Count; }
		}

		private void CheckColumns (ICollection whatever)
		{
			if (n_cols == -1) {

				n_cols = whatever.Count;
				if (n_cols == 0)
					throw new Exception ("Can't have zero columns!");

				alignment = new Alignment [n_cols];
				for (int i = 0; i < n_cols; ++i)
					alignment [i] = Alignment.Right;

				stringify = new Stringify [n_cols];
				
			} else if (n_cols != whatever.Count) {
				throw new Exception (String.Format ("Expected {0} columns, got {1}", n_cols, whatever.Count));
			}
		}
		
		public void AddHeaders (params string [] args)
		{
			CheckColumns (args);
			headers = args;
		}

		public void AddRow (params object [] row)
		{
			CheckColumns (row);
			rows.Add (row);
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
			if (0 <= i && i < n_cols-1)
				return Separator;
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
			int n_rows;
			n_rows = rows.Count;
			if (n_rows > MaxRows)
				n_rows = MaxRows;

			int [] max_width;
			max_width = new int [n_cols];

			if (headers != null)
				for (int i = 0; i < headers.Length; ++i)
					max_width [i] = headers [i].Length;

			string [][][] grid;
			grid = new string [n_rows] [][];

			for (int r = 0; r < n_rows; ++r) {
				object [] row = (object []) rows [r];
				grid [r] = new string [n_cols] [];
				for (int c = 0; c < n_cols; ++c) {
					string str;
					str = stringify [c] != null ? stringify [c] (row [c]) : row [c].ToString ();
					grid [r] [c] = str.Split ('\n');

					foreach (string part in grid [r] [c])
						if (part.Length > max_width [c])
							max_width [c] = part.Length;
				}
			}

			StringBuilder sb, line;
			sb = new StringBuilder ();
			line = new StringBuilder ();

			if (headers != null) {
				sb.Append (GetColumnSeparator (-1));
				for (int i = 0; i < n_cols; ++i) {
					sb.Append (Pad (max_width [i], Alignment.Center, headers [i]));
					sb.Append (GetColumnSeparator (i));
				}
				sb.Append ('\n');
			}

			for (int r = 0; r < n_rows; ++r) {

				bool did_something = true;
				int i = 0;

				if (SkipLines && (r != 0 || headers != null))
				    sb.Append ('\n');
				    
				while (did_something) {
					did_something = false;

					line.Length = 0;
					line.Append (GetColumnSeparator (-1));
					for (int c = 0; c < n_cols; ++c) {
						string str = "";
						if (i < grid [r] [c].Length) {
							str = grid [r][c][i];
							did_something = true;
						}
						str = Pad (max_width [c], alignment [c], str);
						line.Append (str);
						line.Append (GetColumnSeparator (c));
					}
					
					if (did_something) {
						if (r != 0 || i != 0)
							sb.Append ('\n');
						sb.Append (line);
					}
					++i;
				}
			}

			return sb.ToString ();
		}
	}
}
