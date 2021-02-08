using System;
using System.Collections.Generic;
using System.Text;
using Aura.ViewModels;

namespace Aura.Messages
{
	internal class PairServiceWaitMessage
	{
		public PairServiceWaitMessage (PairServiceViewModel vm)
		{
			Context = vm;
		}

		public PairServiceViewModel Context { get; }
	}
}
