using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.FreeSound.API
{
	public class FreeSoundInstance
	{
		public string Id { get; set; }
		public string Url { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string License { get; set; }
		public string Username { get; set; }
		public IReadOnlyList<string> Tags { get; set; }
		
		public string Download { get; set; }
		public string MD5 { get; set; }
		public long Filesize { get; set; }
		public IReadOnlyDictionary<string, string> Previews { get; set; }

		public string Pack { get; set; }
		public string PackTokenized { get; set; }

		public string Type { get; set; }
		public int Channels { get; set; }
		public double Duration { get; set; }
		public decimal SampleRate { get; set; }
		
		//public int BitRate { get; set; }
		//public int BitDepth { get; set; }
	}
}
