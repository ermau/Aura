using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Aura.Data;
using System.Threading.Tasks;

namespace Aura
{
	internal static class SyncExtensions
	{
		public static Task<IReadOnlyList<T>> FindElementsAsync<T> (this ISyncService syncService, string search)
			where T : NamedElement
		{
			return syncService.FindElementsAsync<T> (t =>
				t.Name.ToLower().Contains (search));

			// TODO: Tags
		}
	}
}
