using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public enum AudioChannels
	{
		Mono = 1,
		Stereo = 2,

	}

	public record AudioSample
		: FileSample
	{
		public AudioChannels Channels
		{
			get;
			init;
		}

		public decimal Frequency
		{
			get;
			init;
		}
	}
}
