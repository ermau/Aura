using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public class Layer
		: CampaignElement
	{
		public IList<EnvironmentElement> Elements
		{
			get;
			set;
		}
	}
}
