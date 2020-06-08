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
		: ViewModelBase
	{
		public JoinCampaignDialogViewModel (ILiveCampaignClient liveClient)
		{
			this.join = new RelayCommand (OnJoin, CanJoin);
			this.liveClient = liveClient ?? throw new ArgumentNullException (nameof (liveClient));
		}

		public string Input
		{
			get => this.input;
			set
			{
				if (this.input == value)
					return;

				this.input = value;
				RaisePropertyChanged ();
				this.join.RaiseCanExecuteChanged ();
			}
		}

		public ICommand JoinCommand => this.join;

		public bool IsBusy
		{
			get => this.isBusy;
			set
			{
				if (this.isBusy == value)
					return;

				this.isBusy = false;
				RaisePropertyChanged ();
			}
		}

		public string Error
		{
			get => this.error;
			set
			{
				if (this.error == value)
					return;

				this.error = value;
				RaisePropertyChanged ();
			}
		}

		private bool isBusy;
		private string input, error;
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
				RemoteCampaign campaign = await this.liveClient.GetCampaignDetailsAsync (new Uri (Input), this.cancelSource.Token);
				MessengerInstance.Send (new RequestJoinCampaignMessage (campaign));
			} catch (OperationCanceledException) {
			} finally {
				IsBusy = false;
			}
		}
	}
}
