using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record LayerElement
		: CampaignChildElement
	{
		public IReadOnlyList<string> EnvironmentElements
		{
			get;
			init;
		}
	}
}
