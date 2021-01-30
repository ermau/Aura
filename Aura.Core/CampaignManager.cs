using System.Linq;
using System.Threading.Tasks;

using Aura.Data;
using Aura.Messages;
using Aura.Service;

using GalaSoft.MvvmLight.Messaging;

namespace Aura
{
	internal class CampaignManager
		: SingleSelectionManager<CampaignElement>
	{
		public CampaignManager (ISyncService syncProvider)
			: base (syncProvider)
		{
			Messenger.Default.Register<JoinCampaignMessage> (this, OnJoinCampaign);
		}

		protected override CampaignElement NoSelectionElement => NoSelectionCampaign;

		private static readonly CampaignElement NoSelectionCampaign = new CampaignElement { Name = "Campaigns" };

		private async void OnJoinCampaign (JoinCampaignMessage joinRequest)
		{
			await JoinCampaignAsync (joinRequest);
		}

		private async void OnConnectCampaign (JoinConnectCampaignMessage connectRequest)
		{
			await JoinCampaignAsync (connectRequest);
			Messenger.Default.Send (new ConnectCampaignMessage (connectRequest.Campaign));
		}

		private async Task JoinCampaignAsync (RemoteCampaignMessage message)
		{
			CampaignElement campaign = await CreateCampaignAsync (message.Campaign);
			SelectedElement = campaign;
		}

		internal async Task<CampaignElement> CreateCampaignAsync (RemoteCampaign campaign)
		{
			CampaignElement c = Elements.FirstOrDefault (e => e.Id == campaign.id.ToString ());
			if (c != null)
				return c;

			c = new CampaignElement {
				Id = campaign.id.ToString(),
				IsRemote = true,
				Name = campaign.Name,
				Secret = campaign.Secret,
			};

			await SyncService.SaveElementAsync (c);
			NotifyAddElement (c);
			return c;
		}
	}
}
