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

		protected override EditSingleSelectionElementViewModel<CampaignElement> CreateViewModel (IAsyncServiceProvider services, CampaignElement element)
		{
			return new EditSingleSelectionElementViewModel<CampaignElement> (services, element);
		}

		protected override async Task<SingleSelectionManager<CampaignElement>> GetManagerAsync ()
		{
			return await Services.GetServiceAsync<CampaignManager> ();
		}

		private CampaignManager manager;
		private EditSingleSelectionElementViewModel<CampaignElement> selectedCampaign;

		private void OnDelete ()
		{
			SelectedElement?.DeleteCommand.Execute (null);
		}

		private bool CanDelete () => !(SelectedElement is null);
	}
}
