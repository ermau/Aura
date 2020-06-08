using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Messages
{
	internal class ServiceDiscoveredMesage
	{
		public ServiceDiscoveredMesage (IDiscoverableService service)
		{
			Service = service ?? throw new ArgumentNullException (nameof (service));
		}

		public IDiscoverableService Service { get; }
	}

	internal class EnableServiceMessage
	{
		public EnableServiceMessage (IService service)
		{
			Service = service ?? throw new ArgumentNullException (nameof (service));
		}

		public IService Service { get; }
	}
}
