using System;
using System.Collections.Generic;
using System.Text;

namespace Aura
{
	internal static class CollectionExtensions
	{
		public static void AddRange<T> (this ICollection<T> self, IEnumerable<T> items)
		{
			if (self is null)
				throw new ArgumentNullException (nameof (self));
			if (items is null)
				throw new ArgumentNullException (nameof (items));

			if (self is ObservableCollectionEx<T> obv)
				obv.AddRange (items);
			else if (self is List<T> list)
				list.AddRange (items);
			else {
				foreach (T element in items)
					self.Add (element);
			}
		}
	}
}
