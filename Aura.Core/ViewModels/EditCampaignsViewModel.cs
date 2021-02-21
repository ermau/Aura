using System.Threading.Tasks;
using System.Windows.Input;

using Aura.Data;
using Aura.Messages;

using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class EditCampaignsViewModel
		: EditSingleSelectionItemsViewModel<CampaignElement, EditSingleSelectionElementViewModel<CampaignElement>>
	{
		public EditCampaignsViewModel (IAsyncServiceProvider services)
			: base (services)
		{
			DeleteCampaign = new RelayCommand (OnDelete, CanDelete);
			JoinCampaign = new RelayCommand (() => MessengerInstance.Send (new RequestJoinCampaignPromptMessage ()));
			CreateCampaign = new RelayCommand (() => MessengerInstance.Send (new RequestCreateCampaignPromptMessage ()));
		}

		public ICommand DeleteCampaign
		{
			get;
		}

		public ICommand JoinCampaign
		{
			get;
		}

		public ICommand CreateCampaign
		{
			get;
		}

		protected override CampaignElement InitializeElement (string name)
		{
			return new CampaignElement { Name = name };
		}

		protected override EditSingleSelectionElementViewModel<CampaignElement> InitializeElementViewModel (CampaignElement element)
		{
			return new EditSingleSelectionElementViewModel<CampaignElement> (ServiceProvider, SyncService, element);
		}

		protected override async Task<SingleSelectionManager<CampaignElement>> GetManagerAsync ()
		{
			return await ServiceProvider.GetServiceAsync<CampaignManager> ();
		}
	}
}
