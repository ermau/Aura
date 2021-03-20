using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Aura
{
	internal class PlayGamePage
		: Page
	{
		public PlayGamePage()
		{
			this.setupTask = SetupAsync ();
		}

		protected override async void OnNavigatedTo (NavigationEventArgs e)
		{
			base.OnNavigatedTo (e);

			await this.setupTask;
			UpdateContent ();
		}

		private readonly Task setupTask;
		private CampaignManager campaigns;

		private async Task SetupAsync()
		{
			this.campaigns = await App.Services.GetServiceAsync<CampaignManager> ();
			this.campaigns.PropertyChanged += OnCampaignChanged;
		}

		private void UpdateContent()
		{
			if (this.campaigns.SelectedElement == null)
				return;

			bool running = !this.campaigns.SelectedElement.IsRemote || this.campaigns.SelectedElement.Secret != default;
			if (running) {
				if (!(Content is RunGameView))
					Content = new RunGameView ();
			} else {
				if (!(Content is JoinGameView))
					Content = new JoinGameView ();
			}
		}

		private void OnCampaignChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof (CampaignManager.SelectedElement))
				return;

			UpdateContent ();
		}
	}
}
