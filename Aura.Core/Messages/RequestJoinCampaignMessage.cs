using System;
using System.Collections.Generic;
using System.Text;
using Aura.Service;

namespace Aura.Messages
{
	internal class RequestJoinCampaignMessage
	{
		public RequestJoinCampaignMessage (RemoteCampaign campaign)
		{
			Campaign = campaign ?? throw new ArgumentNullException (nameof (campaign));
		}

		public RemoteCampaign Campaign
		{
			get;
		}
	}
}
