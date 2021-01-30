using System;
using System.Collections.Generic;
using System.Text;

using Aura.Data;

namespace Aura.ViewModels
{
	internal class EnvironmentElementViewModel<T>
		: ElementViewModel<T>
		where T : EnvironmentElement
	{
		public EnvironmentElementViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService, T element)
			: base (serviceProvider, syncService, element)
		{

		}


	}
}
