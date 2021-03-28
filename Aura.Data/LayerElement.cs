using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record LayerElement
		: CampaignChildElement
	{
		[ElementId]
		public IReadOnlyList<string> EnvironmentElements
		{
			get;
			init;
		}
	}
}
