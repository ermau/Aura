using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record ElementPlaylist
	{
		[ElementId]
		public IReadOnlyList<string> Descriptors
		{
			get;
			init;
		} = Array.Empty<string>();

		public SourceOrder Order
		{
			get;
			init;
		}

		public SourceRepeatMode Repeat
		{
			get;
			init;
		}
	}
}