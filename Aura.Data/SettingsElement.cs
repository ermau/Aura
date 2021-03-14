using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record SettingsElement
		: Element
	{
		public bool SpoilerFree { get; init; } = true;
		public bool VisualizePlayback { get; init; } = true;

		public bool AutodetectPlaySpace { get; init; } = true;

		public bool DownloadInBackground { get; init; } = true;
	}
}
