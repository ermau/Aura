using System;
using System.Threading.Tasks;
using Aura.Service;
using Aura.Service.Client;
using Aura.ViewModels;

using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace Aura
{
	public sealed partial class JoinCampaignDialog : ContentDialog
	{
		public JoinCampaignDialog ()
		{
			InitializeComponent ();
			Setup ();
		}

		private void OnCancel (ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			Hide ();
		}

		private async void Setup()
		{
			ILiveCampaignClient liveClient = await App.Services.GetServiceAsync<ILiveCampaignClient> ();
			DataContext = new JoinCampaignDialogViewModel (liveClient);

			await TryPasteLinkAsync ();
		}

		private async Task TryPasteLinkAsync()
		{
			DataPackageView dataView = Clipboard.GetContent ();
			(string copiedUrl, _) = await dataView.TryGetLinkAsync ();
			if (copiedUrl != null && String.IsNullOrEmpty (this.text.Text)) {
				((JoinCampaignDialogViewModel)DataContext).Input = copiedUrl;
			}
		}
	}
}
