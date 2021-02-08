using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Messages
{
	internal class PairServiceMessage
	{
		public PairServiceMessage (IPairedService pairedService)
		{
			PairedService = pairedService;
		}

		public IPairedService PairedService { get; }

		public Task<(PairingOption, string)> Result { get; set; }
	}
}
