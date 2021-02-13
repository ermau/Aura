using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aura
{
	public interface IAuthenticationService
	{
		Task<bool> TryAuthenticateAsync (IAuthenticatedService service, CancellationToken cancellationToken);
		Task<bool> AuthenticateAsync (IAuthenticatedService service);
		
		Task LogoutAsync (IAuthenticatedService service);
	}
}
