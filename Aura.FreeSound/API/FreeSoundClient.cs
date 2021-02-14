using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aura.FreeSound.API
{
	public class FreeSoundClient
	{
		public FreeSoundClient (string clientId, string clientSecret)
		{
			if (string.IsNullOrWhiteSpace (clientId))
				throw new ArgumentException ($"'{nameof (clientId)}' cannot be null or whitespace", nameof (clientId));
			if (string.IsNullOrWhiteSpace (clientSecret))
				throw new ArgumentException ($"'{nameof (clientSecret)}' cannot be null or whitespace", nameof (clientSecret));

			this.clientId = clientId;
			this.clientSecret = clientSecret;
		}

		public bool IsLoggedIn => this.client.DefaultRequestHeaders.Authorization != null;

		public void SetAccessToken (string accessToken)
		{
			if (string.IsNullOrWhiteSpace (accessToken))
				throw new ArgumentException ($"'{nameof (accessToken)}' cannot be null or whitespace", nameof (accessToken));

			this.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue ("Bearer", accessToken);
		}

		public Task<OAuthAuthenticationResult> ExchangeAsync (string authenticationCode, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace (authenticationCode))
				throw new ArgumentException ($"'{nameof (authenticationCode)}' cannot be null or whitespace", nameof (authenticationCode));

			return AuthCoreAsync (AuthType.Code, authenticationCode, cancellationToken);
		}

		public void Logout ()
		{
			this.client.DefaultRequestHeaders.Authorization = null;
		}

		public async Task<FreeSoundUser> GetMeAsync(CancellationToken cancellationToken)
		{
			if (this.client.DefaultRequestHeaders.Authorization == null)
				return null;

			var result = await this.client.GetAsync ($"{ApiRoot}me/", cancellationToken).ConfigureAwait (false);
			return JsonConvert.DeserializeObject<FreeSoundUser> (await result.Content.ReadAsStringAsync ());
		}

		public Task<OAuthAuthenticationResult> RefreshAsync  (string refreshToken, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace (refreshToken))
				throw new ArgumentException ($"'{nameof (refreshToken)}' cannot be null or whitespace", nameof (refreshToken));

			return AuthCoreAsync (AuthType.Refresh, refreshToken, cancellationToken);
		}

		public async Task<FreeSoundSearchResults> SearchAsync (FreeSoundSearchOptions options, CancellationToken cancellationToken)
		{
			if (options is null)
				throw new ArgumentNullException (nameof (options));

			options.AddField (i => i.Id);
			options.AddField (i => i.Name);
			options.AddField (i => i.Description);
			options.AddField (i => i.License);
			options.AddField (i => i.Duration);
			options.AddField (i => i.Username);

			var result = await this.client.GetAsync ($"{ApiRoot}search/text/{options.GetRequest()}", cancellationToken).ConfigureAwait (false);
			var resultContent = await result.Content.ReadAsStringAsync ().ConfigureAwait (false);
			return JsonConvert.DeserializeObject<FreeSoundSearchResults> (resultContent);
		}

		public async Task<FreeSoundInstance> GetAsync (string soundId, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace (soundId))
				throw new ArgumentException ($"'{nameof (soundId)}' cannot be null or whitespace", nameof (soundId));

			var result = await this.client.GetAsync ($"{ApiRoot}sounds/{soundId}/", cancellationToken);
			return JsonConvert.DeserializeObject<FreeSoundInstance> (await result.Content.ReadAsStringAsync ());
		}

		public Task<Stream> DownloadSoundAsync (string soundId)
		{
			return this.client.GetStreamAsync (new Uri ($"{ApiRoot}sounds/{soundId}/download/"));
		}

		private readonly HttpClient client = new HttpClient ();
		private readonly string clientId;
		private readonly string clientSecret;
		private const string ApiRoot = "https://freesound.org/apiv2/";

		private enum AuthType
		{
			Code,
			Refresh
		}

		private async Task<OAuthAuthenticationResult> AuthCoreAsync (AuthType type, string token, CancellationToken cancellationToken)
		{
			var form = new MultipartFormDataContent ();
			form.Add (new StringContent (this.clientId), "client_id");
			form.Add (new StringContent (this.clientSecret), "client_secret");
			form.Add (new StringContent ((type == AuthType.Code) ? "authorization_code" : "refresh_token"), "grant_type");
			form.Add (new StringContent (token), (type == AuthType.Code) ? "code": "refresh_token");

			var result = await client.PostAsync (new Uri ($"{ApiRoot}oauth2/access_token/"), form, cancellationToken).ConfigureAwait (false);
			var jobj = JObject.Parse (await result.Content.ReadAsStringAsync ().ConfigureAwait (false));

			var authResult = new OAuthAuthenticationResult {
				AccessToken = (string)jobj["access_token"],
				RefreshToken = (string)jobj["refresh_token"],
				Scope = (string)jobj["scope"],
				ExpiresAt = DateTime.Now + TimeSpan.FromMilliseconds ((int)jobj["expires_in"] - 5)
			};

			cancellationToken.ThrowIfCancellationRequested ();
			SetAccessToken (authResult.AccessToken);
			return authResult;
		}
	}
}
