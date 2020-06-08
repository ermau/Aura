
using Aura.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Aura
{
	public sealed partial class EditCampaignsPage : Page
	{
		public EditCampaignsPage ()
		{
			InitializeComponent ();
			LoadAsync ();
		}

		private async void LoadAsync()
		{
			ISyncService sync = await App.Services.GetServiceAsync<ISyncService> ();
			CampaignManager campaigns = await App.Services.GetServiceAsync<CampaignManager> ();
			DataContext = new EditCampaignsViewModel (sync, campaigns);
		}
	}
}
