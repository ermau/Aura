using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Data;

namespace Aura.FreeSound
{
	// TODO: Auth code expiration

	[Export (typeof (IAuthenticatedService))]
	[Export (typeof (IContentProviderService))]
	[Shared]
	public class FreeSoundService
		: IOAuthedService, IContentProviderService
	{
		public FreeSoundService ()
		{
			this.client = new API.FreeSoundClient (ClientId, ClientSecret);
		}

		public string DisplayName => "freesound.org";

		public string ClientId => "";
		public string ClientSecret => "";
		public Uri AuthUri => new Uri($"https://freesound.org/apiv2/oauth2/authorize/?client_id={ClientId}&response_type=code");
		public Uri CallbackUri => new Uri("https://freesound.org/home/app_permissions/permission_granted/");

		public string RequestedPermissions => "";

		public bool IsLoggedIn => this.client.IsLoggedIn;

		public async Task<ContentEntry> GetEntryAsync (string contentId, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace (contentId))
				throw new ArgumentException ($"'{nameof (contentId)}' cannot be null or whitespace", nameof (contentId));

			var instance = await this.client.GetAsync (contentId, cancellationToken);
			return ToEntry (instance);
		}

		public Task<Stream> DownloadEntryAsync (string contentId)
		{
			if (string.IsNullOrWhiteSpace (contentId))
				throw new ArgumentException ($"'{nameof (contentId)}' cannot be null or whitespace", nameof (contentId));

			return this.client.DownloadSoundAsync (contentId);
		}

		public async Task<ContentPage> SearchAsync (ContentSearchOptions options, CancellationToken cancellationToken)
		{
			if (options is null)
				throw new ArgumentNullException (nameof (options));

			var search = await this.client.SearchAsync (new API.FreeSoundSearchOptions {
				Query = options.Query,
				Page = options.Page
			}, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();

			return new ContentPage {
				Entries = search.Results.Select (ToEntry).ToList ()
			};
		}

		public async Task<string> GetUsernameAsync(CancellationToken cancellationToken)
		{
			return (await this.client.GetMeAsync (cancellationToken))?.Username;
		}

		public Task<OAuthAuthenticationResult> AuthenticateAsync (string authenticationCode, CancellationToken cancellationToken)
		{
			return this.client.ExchangeAsync (authenticationCode, cancellationToken);
		}

		public Task<OAuthAuthenticationResult> RefreshTokenAsync (string refreshToken, CancellationToken cancellationToken)
		{
			return this.client.RefreshAsync (refreshToken, cancellationToken);
		}

		public Task LogoutAsync()
		{
			this.client.Logout ();
			return Task.CompletedTask;
		}

		public string GetCode (string responseData)
		{
			if (String.IsNullOrWhiteSpace (responseData))
				return null;

			int index = responseData.IndexOf ("code=");
			if (index == -1)
				return null;

			return responseData.Substring (index + 5, responseData.Length - index - 5);
		}

		private readonly API.FreeSoundClient client;

		private ContentEntry ToEntry (API.FreeSoundInstance instance)
		{
			return new ContentEntry {
				Id = instance.Id,
				SourceUrl = $"https://freesound.org/sounds/{instance.Id}/",
				Name = instance.Name,
				Description = instance.Description,
				Author = new ContentAuthor {
					Name = instance.Username,
					Url = $"https://freesound.org/people/{instance.Username}/"
				},
				Duration = TimeSpan.FromSeconds (instance.Duration),
				License = instance.License,
				Size = instance.Filesize,
				Previews = instance.Previews?.Select (kvp => new ContentEntryPreview { Url = kvp.Value }).ToList()
			};
		}
	}
}
