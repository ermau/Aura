using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aura
{
	public interface IAsyncServiceProvider
	{
		Task<T> GetServiceAsync<T> ();
		Task<T[]> GetServicesAsync<T> ();
	}
}
