using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record EnvironmentElement
		: CampaignChildElement
	{
		public Timing Timing
		{
			get;
			init;
		}

		public Positioning Positioning
		{
			get;
			init;
		}
	}
}