using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Messages
{
	public class CampaignDisconnectedMessage
	{
		public CampaignDisconnectedMessage (Guid campaignId)
		{
			CampaignId = campaignId;
		}

		public Guid CampaignId
		{
			get;
		}
	}

	public class CampaignReconnectedMessage
	{
		public CampaignReconnectedMessage (Guid campaignId)
		{
			CampaignId = campaignId;
		}

		public Guid CampaignId
		{
			get;
		}
	}
}
