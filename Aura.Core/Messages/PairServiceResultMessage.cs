using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Messages
{
	internal class PairServiceResultMessage
	{
		public PairServiceResultMessage (IPairedService service)
		{
			Service = service ?? throw new ArgumentNullException (nameof (service));
		}

		public PairServiceResultMessage (IPairedService service, Exception ex)
			: this (service)
		{
			Exception = ex;
		}

		public Exception Exception { get; }

		public IPairedService Service { get; }
	}
}
