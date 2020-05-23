using System.Threading.Tasks;

using Aura.Messages;
using Aura.Service;

using GalaSoft.MvvmLight.Messaging;

namespace Aura
{
	internal class CampaignManager
		: SingleSelectionManager<Campaign>
	{
		public CampaignManager (ISyncService syncProvider)
			: base (syncProvider)
		{
			Messenger.Default.Register<RequestJoinCampaignMessage> (this, OnJoinCampaign);
		}

		protected override Campaign NoSelectionElement => NoSelectionCampaign;

		private static readonly Campaign NoSelectionCampaign = new Campaign { Name = "Campaigns" };

		private async void OnJoinCampaign (RequestJoinCampaignMessage joinRequest)
		{
			Campaign campaign = await CreateCampaignAsync (joinRequest.Campaign);
			SelectedElement = campaign;
		}

		private async Task<Campaign> CreateCampaignAsync (RemoteCampaign campaign)
		{
			var c = new Campaign {
				Id = campaign.id.ToString(),
				IsRemote = true,
				Name = campaign.Name,
				Secret = campaign.Secret,
			};

			await SyncService.SaveElementAsync (c);
			AddElement (c);
			return c;
		}
	}
}
