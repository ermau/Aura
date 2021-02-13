using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aura
{
	public interface IAuthenticatedService
		: IService
	{
		bool IsLoggedIn { get; }

		Uri AuthUri { get; }

		Task<string> GetUsernameAsync (CancellationToken cancellationToken);
		Task LogoutAsync ();
	}

	public interface IOAuthedService
		: IAuthenticatedService
	{
		string ClientId { get; }
		string ClientSecret { get; }
		Uri CallbackUri { get; }
		
		string RequestedPermissions { get; }

		string GetCode (string responseData);

		Task<OAuthAuthenticationResult> AuthenticateAsync (string authenticationCode, CancellationToken cancellationToken);
		Task<OAuthAuthenticationResult> RefreshTokenAsync (string refreshToken, CancellationToken cancellationToken);
	}

	public record OAuthAuthenticationResult
	{
		public string AccessToken;
		public string Scope;
		public string RefreshToken;
		public DateTime ExpiresAt;
	}
}
