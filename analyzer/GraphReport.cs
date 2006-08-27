using Cairo;
using Glade;
using Gtk;
using System;
using System.Collections;
using System.IO;
using Mono.Unix.Native;

namespace HeapBuddy {
	
	public class GraphReport : Report {
	
		public GraphReport () : base ("Graph") { }

		private MemGraph Graph;
		private OutfileReader Reader;
		
		[Widget] VBox vbox1;
		[Widget] Gtk.Window MainWindow;
		
		override public void Run (OutfileReader reader, string [] args)
		{
			Reader = reader;
		
			Application.Init ();
		
			Glade.XML gxml = new Glade.XML (null, "memgraph.glade", "MainWindow", null);
			gxml.Autoconnect (this);
			
			Graph = new MemGraph ();
			PopulateGraph ();
			
			vbox1.Add (Graph);
			
			MainWindow.ShowAll ();
			
			Application.Run ();
		}
		
		private void PopulateGraph ()
		{
			Stream stream = new FileStream ("outfile.types", FileMode.Open, FileAccess.Read);
			BinaryReader reader = new BinaryReader (stream);
			
			while (reader.PeekChar () != -1) {
				Timeval t = new Timeval ();
				t.tv_sec = reader.ReadInt64 ();
				t.tv_usec = reader.ReadInt64 ();
							
				uint count = reader.ReadUInt32 ();
				int bytes;
				string name;
				
				for (; count > 0; count--) {
					bytes = reader.ReadInt32 ();
					name = reader.ReadString ();
					
					Graph.AddStamp (new MemStamp (t, name, bytes));
				}
			}
			
			foreach (Gc gc in Reader.Gcs) {
				Graph.AddStamp (new MemStamp (gc.TimeT, gc.PreGcLiveBytes));
			}
			
			Graph.Sort ();
						
			reader.Close ();
		}
		
		protected void SaveGraph (object o, EventArgs e)
		{
		
		}
		
		protected void QuitApplication (object o, EventArgs e)
		{
			Application.Quit ();
		}
	
	}
	
}
