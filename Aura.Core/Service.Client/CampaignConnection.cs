using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Data;
using Aura.Service.Messages;
using Microsoft.AspNetCore.SignalR.Client;

namespace Aura.Service.Client
{
	public interface ICampaignConnection
	{
		Task StartGameAsync (Guid campaignSecret, CancellationToken cancellationToken);

		Task DisconnectAsync ();
	}

	public class CampaignNotFoundException
		: Exception
	{
	}

	public class CampaignConnection
		: ICampaignConnection, INotifyPropertyChanged
	{
		internal CampaignConnection (string campaignId, HubConnection connection, IAsyncServiceProvider serviceProvider)
		{
			if (string.IsNullOrWhiteSpace (campaignId))
				throw new ArgumentException ($"'{nameof (campaignId)}' cannot be null or whitespace", nameof (campaignId));

			this.campaignId = campaignId;
			this.connection = connection;
			this.serviceProvider = serviceProvider;
			this.connection.Closed += OnConnectionClosed;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public IReadOnlyCollection<CampaignParticipant> Participants
		{
			get => this.participants.Values.ToList ();
		}

		public async Task StartGameAsync (Guid campaignSecret, CancellationToken cancellationToken)
		{
			StartGameResult result = await this.connection.InvokeAsync<StartGameResult> ("StartGame", new StartGameMessage {
				CampaignId = this.campaignId,
				CampaignSecret = campaignSecret.ToString ()
			}, cancellationToken).ConfigureAwait (false);

			if (result == StartGameResult.Success)
				return;
			if (result == StartGameResult.Unauthorized)
				throw new UnauthorizedAccessException ();
			if (result == StartGameResult.CampaignNotFound)
				throw new CampaignNotFoundException ();

			throw new Exception ("Unknown error");
		}

		public async Task JoinGameAsync (CancellationToken cancellationToken)
		{
			JoinGameResult result = await this.connection.InvokeAsync<JoinGameResult> ("JoinGame", new JoinGameMessage {
				CampaignId = this.campaignId
			}, cancellationToken).ConfigureAwait (false);

			if (result == JoinGameResult.Success)
				return;
			if (result == JoinGameResult.CampaignNotFound)
				throw new CampaignNotFoundException ();

			throw new Exception ("Unknown error");
		}

		private void OnUpdateParticipants (UpdateParticipantsMessage msg)
		{
			if (msg.Participants == null)
				return;

			foreach (var p in msg.Participants) {
				this.participants.AddOrUpdate (p.Id, p, (s, op) => p);
			}

			PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (nameof (Participants)));
		}

		internal async Task<ICampaignConnection> StartAsync(CancellationToken cancelToken)
		{
			await this.connection.StartAsync (cancelToken).ConfigureAwait(false);
			return this;
		}

		public Task<IPreparedEffect> PrepareElementAsync (EnvironmentElement element, PlaybackOptions options, CancellationToken cancellationToken)
		{
			if (element is null)
				throw new ArgumentNullException (nameof (element));
			if (options is null)
				throw new ArgumentNullException (nameof (options));
			if (this.connection == null)
				throw new InvalidOperationException ("Not yet connected to a campaign");

			throw new NotImplementedException ();
		}

		public Task DisconnectAsync ()
		{
			var c = this.connection;
			if (c == null)
				return Task.CompletedTask;

			return c.DisposeAsync ().AsTask();
		}

		private readonly string campaignId;
		private readonly HubConnection connection;
		private readonly IAsyncServiceProvider serviceProvider;
		private readonly ConcurrentDictionary<string, CampaignParticipant> participants = new ConcurrentDictionary<string, CampaignParticipant> ();

		private Task OnConnectionClosed (Exception arg)
		{
			return Task.CompletedTask;
		}

		private void OnPlayLayerAsync (PlayLayerMessage msg)
		{
			throw new NotImplementedException ();
		}

		private void OnPrepareLayerAsync (PrepareLayerMessage msg)
		{
			throw new NotImplementedException ();
		}
	}
}
