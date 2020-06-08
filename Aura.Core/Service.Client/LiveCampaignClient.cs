using System;
using System.Net;
using System.Net.Http;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

using Aura.Service.Messages;
using Aura.Service.Client;
using Newtonsoft.Json;

namespace Aura.Service
{
	[Export (typeof(ILiveCampaignClient))]
	public class LiveCampaignClient
		: ILiveCampaignClient
	{
		public static bool IsLiveUri (string url)
		{
			if (!Uri.TryCreate (url, UriKind.Absolute, out Uri uri))
				return false;

			return IsLiveUri (uri);
		}

		public static bool IsJoinUri (string url)
		{
			if (!Uri.TryCreate (url, UriKind.Absolute, out Uri uri))
				return false;

			return IsJoinUri (uri);
		}

		public static bool IsJoinUri (Uri uri)
		{
			if (!IsLiveUri (uri))
				return false;

			return true;
		}

		public static bool IsLiveUri (Uri uri) => uri.AbsoluteUri.StartsWith (baseUri);

		public async Task<RemoteCampaign> CreateCampaignAsync (string name, CancellationToken cancelToken)
		{
			try {
				HttpResponseMessage response = await this.client.GetAsync (new Uri (baseUri + "campaigns/create?name=" + name), cancelToken).ConfigureAwait (false);
				if (!response.IsSuccessStatusCode)
					return null;

				string responseContent = await response.Content.ReadAsStringAsync ().ConfigureAwait (false);
				return JsonConvert.DeserializeObject<RemoteCampaign> (responseContent);
			} catch (WebException) {
				return null;
			}
		}

		public async Task<RemoteCampaign> GetCampaignDetailsAsync (Uri uri, CancellationToken cancelToken)
		{
			if (uri is null)
				throw new ArgumentNullException (nameof (uri));

			try {
				HttpResponseMessage response = await this.client.GetAsync (uri, cancelToken).ConfigureAwait (false);
				if (!response.IsSuccessStatusCode)
					return null;

				string responseContent = await response.Content.ReadAsStringAsync ().ConfigureAwait (false);
				return JsonConvert.DeserializeObject<RemoteCampaign> (responseContent);
			} catch (WebException) {
				return null;
			}
		}

		public Task<RemoteCampaign> GetCampaignDetailsAsync (string id, CancellationToken cancelToken)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ("message", nameof (id));

			return GetCampaignDetailsAsync (new Uri (baseUri + "campaigns/" + id), cancelToken);
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
		//http://localhost:7071/api/campaigns/ed2fbdad-2ce1-43a0-9ce0-31e3e6d3ed14
		private const string baseUri = "http://localhost:7071/api/";
		private readonly HttpClient client = new HttpClient ();
		
		private HubConnection connection;

		private void OnPrepareLayer (PrepareLayerMessage msg)
		{

		}

		private void OnPlayLayer (PlayLayerMessage msg)
		{

		}
	}
}
