using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Aura
{
	internal static class WinExtensions
	{
		public static ConfiguredTaskAwaitable<T> ConfigureAwait<T> (this IAsyncOperation<T> self, bool useSyncContext)
		{
			if (self == null)
				throw new ArgumentNullException (nameof (self));

			return self.AsTask ().ConfigureAwait (useSyncContext);
		}
	}
}
