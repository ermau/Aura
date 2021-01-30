using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

using Aura.Data;
using Aura.Messages;
using Aura.Service.Client;

namespace Aura.ViewModels
{
	internal class JoinPlayPageViewModel
		: ViewModelBase
	{
		public JoinPlayPageViewModel (IAsyncServiceProvider services)
		{
			this.services = services ?? throw new ArgumentNullException (nameof (services));
			SetupAsync ();

			MessengerInstance.Register<SingleSelectionPreviewChangeMessage> (this, OnSingleSelectionChangedPreview);
			MessengerInstance.Register<SingleSelectionChangedMessage> (this, m => RaisePropertyChanged (nameof (Campaign)));

			JoinLive = new RelayCommand (() => Join());
			CancelJoin = new RelayCommand (() => this.joinCancel?.Cancel(), () => this.joinCancel != null);
		}

		public string Status
		{
			get
			{
				if (IsBusy) {
					return "Joining " + Campaign.Name + "...";
				} else {
					return Campaign?.Name;
				}
			}
		}

		public ICommand JoinLive
		{
			get;
		}

		public ICommand CancelJoin
		{
			get;
		}

		public bool IsBusy
		{
			get => this.busy;
			private set
			{
				if (this.busy == value)
					return;

				this.busy = value;
				RaisePropertyChanged ();
				RaisePropertyChanged (nameof (Status));
			}
		}

		public CampaignElement Campaign => this.campaigns?.SelectedElement;

		private readonly IAsyncServiceProvider services;
		private CampaignManager campaigns;
		private Task<ILiveCampaignClient> clientTask;
		private bool joined;
		private bool busy;
		private CancellationTokenSource joinCancel;
		private ICampaignConnection connection;

		private async void Join()
		{
			if (this.joinCancel != null) {
				this.joinCancel.Cancel ();
			}

			this.joinCancel = new CancellationTokenSource ();
			CancellationToken token = this.joinCancel.Token;
			((RelayCommand)CancelJoin).RaiseCanExecuteChanged ();
			IsBusy = true;

			if (!Campaign.IsRemote)
				throw new InvalidOperationException ();

			var client = await this.clientTask.ConfigureAwait(false);
			try {
				this.connection = await client.ConnectToCampaignAsync (Campaign.Id, this.services, token);
				if (Campaign.Secret != default) {
					await this.connection.StartGameAsync (Campaign.Secret, token);
				}
			} catch (OperationCanceledException) {
				this.connection?.DisconnectAsync ();
				this.connection = null;
			} finally {
				IsBusy = false;
			}
		}

		private void OnConnectionClosed (object sender, EventArgs e)
		{
			throw new NotImplementedException ();
		}

		private async void Disconnect()
		{
			var cancel = this.joinCancel;
			if (Interlocked.CompareExchange (ref this.joinCancel, null, cancel) == cancel)
				cancel.Cancel ();

			try {
				var c = this.connection;
				await c.DisconnectAsync ();
			} finally {
				this.joined = false;
				IsBusy = false;
			}
		}

		private void OnCampaignChanged()
		{
			RaisePropertyChanged (nameof (Campaign));
			RaisePropertyChanged (nameof (Status));
		}

		private async void SetupAsync()
		{
			this.clientTask = this.services.GetServiceAsync<ILiveCampaignClient> ();
			this.campaigns = await this.services.GetServiceAsync<CampaignManager> ();
			this.campaigns.PropertyChanged += (o, e) => OnCampaignChanged ();
			OnCampaignChanged ();
		}

		private void OnSingleSelectionChangedPreview (SingleSelectionPreviewChangeMessage msg)
		{
			if (msg.Type != typeof (CampaignElement))
				return;

			if (this.joined) {
				var prompt = new PromptMessage ("Switch campaigns?", $"You're currently connected to a campaign, switching campaigns will disconnect you.", "Switch");
				MessengerInstance.Send (prompt);

				msg.Canceled = prompt.Result.ContinueWith (t => {
					if (!t.Result) {
						Disconnect ();
					}

					return !t.Result;
				});
			}
		}
	}
}
