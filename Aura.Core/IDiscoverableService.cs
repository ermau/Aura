using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aura
{
	public interface IDiscoverableService
		: IService
	{
		/// <summary>
		/// Attempts to discover the presence of a service, allowing Aura to prompt the user to enable it.
		/// </summary>
		/// <returns><c>true</c> if the service was found. <c>false</c> is not definitive.</returns>
		Task<bool> DiscoverAsync ();
	}
}
