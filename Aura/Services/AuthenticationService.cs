using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Xamarin.Essentials;

namespace Aura.Services
{
	[Export (typeof (IAuthenticationService)), Shared]
	internal class AuthenticationService
		: IAuthenticationService
	{
		public AuthenticationService()
		{
			var timer = new System.Timers.Timer (TimeSpan.FromMinutes (15).TotalMilliseconds);
			timer.Elapsed += OnTimerElapsed;
			timer.Start ();
		}

		public async Task<bool> TryAuthenticateAsync (IAuthenticatedService service, CancellationToken cancellationToken)
		{
			if (service is null)
				throw new ArgumentNullException (nameof (service));

			if (service.IsLoggedIn)
				return true;

			IOAuthedService oauth = service as IOAuthedService;
			if (oauth == null)
				throw new NotSupportedException ();

			string serviceName = service.GetType ().GetSimpleTypeName ();

			string refreshToken = await SecureStorage.GetAsync (serviceName);
			if (String.IsNullOrWhiteSpace (refreshToken))
				return false;

			try {
				var refreshResult = await oauth.RefreshTokenAsync (refreshToken, cancellationToken);
				if (refreshResult != null) {
					await SecureStorage.SetAsync (serviceName, refreshResult.RefreshToken);
					return true;
				}
			} catch {
			}

			return false;
		}

		public async Task<bool> AuthenticateAsync (IAuthenticatedService service)
		{
			if (service is null)
				throw new ArgumentNullException (nameof (service));

			IOAuthedService oauth = service as IOAuthedService;
			if (oauth == null)
				throw new NotSupportedException ();

			if (await TryAuthenticateAsync (service, CancellationToken.None))
				return true;

			string serviceName = service.GetType ().GetSimpleTypeName ();
			try {
				var result = await WebAuthenticationBroker.AuthenticateAsync (WebAuthenticationOptions.None, oauth.AuthUri, oauth.CallbackUri);
				if (result.ResponseStatus == WebAuthenticationStatus.Success) {
					string code = oauth.GetCode (result.ResponseData);
					var authResult = await oauth.AuthenticateAsync (code, CancellationToken.None);
					if (authResult != null) {
						await SecureStorage.SetAsync (serviceName, authResult.RefreshToken);
						return true;
					}
				}
			} catch {
			}
			
			return false;
		}

		public async Task LogoutAsync (IAuthenticatedService service)
		{
			if (service is null)
				throw new ArgumentNullException (nameof (service));

			string serviceName = service.GetType ().GetSimpleTypeName ();
			SecureStorage.Remove (serviceName);

			await service.LogoutAsync ();
		}

		private void OnTimerElapsed (object sender, System.Timers.ElapsedEventArgs e)
		{
			// TODO: Check for expirations + refresh
		}
	}
}
