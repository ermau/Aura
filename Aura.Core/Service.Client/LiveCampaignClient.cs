using System;
using System.Net;
using System.Net.Http;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Aura.Service.Client;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;


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

		public async Task StartAsync (IAsyncServiceProvider services)
		{
			if (services is null)
				throw new ArgumentNullException (nameof (services));

			this.sync = await services.GetServiceAsync<ISyncService> ();
		}

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

			string url = id;
			if (!IsLiveUri (id))
				url = baseUri + "campaigns/" + id;

			return GetCampaignDetailsAsync (new Uri (url), cancelToken);
		}

		public Task<ICampaignConnection> ConnectToCampaignAsync (string id, IAsyncServiceProvider services, CancellationToken cancelToken)
		{
			if (id == null)
				throw new ArgumentNullException (nameof (id));
			if (services is null)
				throw new ArgumentNullException (nameof (services));
			if (!Guid.TryParse (id, out Guid campaignId))
				throw new ArgumentException (nameof (id));

			HubConnection connection = new HubConnectionBuilder ()
				.WithUrl (baseUri + "CampaignHub")
				.WithAutomaticReconnect ()
				/*.ConfigureLogging(logging => {
					logging.SetMinimumLevel (LogLevel.Debug);
					logging.AddDebug ();
				})*/
				.Build ();

			var campaignConnection = new CampaignConnection (id, connection, services);
			return campaignConnection.StartAsync (cancelToken);
		}

		private const string baseUri = "http://localhost:7071/api/";
		private readonly HttpClient client = new HttpClient ();

		private ISyncService sync;
	}
}
