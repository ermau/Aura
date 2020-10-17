using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Service.Messages
{
	public class StartGameMessage
	{
		public string CampaignId
		{
			get;
			set;
		}

		public string CampaignSecret
		{
			get;
			set;
		}
	}
}
