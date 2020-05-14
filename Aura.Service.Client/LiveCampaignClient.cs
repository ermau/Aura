using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

using Aura.Service.Messages;

namespace Aura.Service
{
	public class LiveCampaignClient
	{
		public static bool IsLiveUrl (string url)
		{
			if (!Uri.TryCreate (url, UriKind.Absolute, out Uri uri))
				return false;

			return IsLiveUri (uri);
		}

		public static bool IsLiveUri (Uri uri) => uri.AbsoluteUri.StartsWith (baseUri);

		public async Task<Campaign> CreateCampaignAsync (string name)
		{
			try {
				string response = await this.client.DownloadStringTaskAsync (new Uri (baseUri + "campaigns/create?name=" + name));
				return JsonSerializer.Deserialize<Campaign> (response);
			} catch (WebException) {
				return null;
			}
		}

		public async Task<Campaign> GetCampaignDetailsAsync (string id)
		{
			try {
				string response = await this.client.DownloadStringTaskAsync (new Uri (baseUri + "campaigns/" + id));
				return JsonSerializer.Deserialize<Campaign> (response);
			} catch (WebException) {
				return null;
			}
		}

		public async Task ConnectToCampaignAsync (string id, CancellationToken cancelToken = default)
		{
			if (id == null)
				throw new ArgumentNullException (nameof (id));
			if (!Guid.TryParse (id, out Guid campaignId))
				throw new ArgumentException (nameof (id));

			this.connection = new HubConnectionBuilder ()
				.WithUrl (baseUri + "/campaigns/" + id)
				.Build ();

			this.connection.On<PrepareLayerMessage> ("PrepareLayer", OnPrepareLayer);
			this.connection.On<PlayLayerMessage> ("PlayLayer", OnPlayLayer);
			
			await this.connection.StartAsync (cancelToken);
		}

		private const string baseUri = "http://localhost:7071/api/";
		private readonly WebClient client = new WebClient ();

		private HubConnection connection;

		private void OnPrepareLayer (PrepareLayerMessage msg)
		{

		}

		private void OnPlayLayer (PlayLayerMessage msg)
		{

		}
	}
}
