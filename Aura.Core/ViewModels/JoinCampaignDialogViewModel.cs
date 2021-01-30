using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Input;
using Aura.Messages;
using Aura.Service;
using Aura.Service.Client;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class JoinCampaignDialogViewModel
		: InputDialogViewModel
	{
		public JoinCampaignDialogViewModel (ILiveCampaignClient liveClient)
		{
			this.join = new RelayCommand (OnJoin, CanJoin);
			this.liveClient = liveClient ?? throw new ArgumentNullException (nameof (liveClient));
		}

		public ICommand JoinCommand => this.join;

		protected override RelayCommand Command => this.join;

		private RelayCommand join;
		private CancellationTokenSource cancelSource;
		private readonly ILiveCampaignClient liveClient;

		private bool CanJoin => LiveCampaignClient.IsLiveUri (Input);

		private async void OnJoin ()
		{
			this.cancelSource?.Cancel ();
			this.cancelSource = new CancellationTokenSource (20000);

			IsBusy = true;
			try {
				RemoteCampaign campaign = await this.liveClient.GetCampaignDetailsAsync (Input, this.cancelSource.Token);
				MessengerInstance.Send (new JoinCampaignMessage (campaign));
			} catch (OperationCanceledException) {
			} finally {
				IsBusy = false;
			}
		}
	}
}
