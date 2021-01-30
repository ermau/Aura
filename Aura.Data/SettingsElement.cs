using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record SettingsElement
		: Element
	{
		public bool SpoilerFree;
		public bool VisualizePlayback;
		
		public bool AutodetectPlaySpace;
	}
}
