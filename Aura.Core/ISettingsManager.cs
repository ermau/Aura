using System;
using System.Collections.Generic;
using System.Text;

namespace Aura
{
	public interface ISettingsManager
	{
		event EventHandler SettingsChanged;

		bool SpoilerFree { get; set; }
		bool VisualizePlayback { get; set; }

		bool AutodetectPlaySpace { get; set; }
	}
}
