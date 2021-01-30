using System;

using Aura.Service;

namespace Aura.Messages
{
	internal class RequestCampaignPromptMessage
	{
	}

	internal class RequestJoinCampaignPromptMessage
	{
	}

	internal class RequestCreateCampaignPromptMessage
	{
	}

	internal class RemoteCampaignMessage
	{
		public RemoteCampaignMessage (RemoteCampaign campaign)
		{
			Campaign = campaign ?? throw new ArgumentNullException (nameof (campaign));
		}

		public RemoteCampaign Campaign
		{
			get;
			set;
		}
	}

	internal class JoinCampaignMessage
		: RemoteCampaignMessage
	{
		public JoinCampaignMessage (RemoteCampaign campaign)
			: base (campaign)
		{
		}
	}

	internal class JoinConnectCampaignMessage
		: RemoteCampaignMessage
	{
		public JoinConnectCampaignMessage (RemoteCampaign campaign)
			: base (campaign)
		{
		}
	}

	internal class ConnectCampaignMessage
		: RemoteCampaignMessage
	{
		public ConnectCampaignMessage (RemoteCampaign campaign)
			: base (campaign)
		{
		}
}
}
