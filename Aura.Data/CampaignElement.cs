using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record CampaignElement
		: NamedElement
	{
		public Guid Secret
		{
			get;
			init;
		}

		public bool IsRemote
		{
			get;
			init;
		}
	}
}
