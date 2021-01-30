using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Service.Messages
{
	public class JoinGameMessage
	{
		public string CampaignId
		{
			get;
			set;
		}
	}
	public class StartGameMessage
		: JoinGameMessage
	{
		public string CampaignSecret
		{
			get;
			set;
		}
	}
}
