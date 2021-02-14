using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Aura
{
	internal class ObservableCollectionEx<T>
		: ObservableCollection<T>
	{
		public void Reset (IEnumerable<T> newItems)
		{
			if (newItems is null)
				throw new ArgumentNullException (nameof (newItems));

			ClearItems ();

			foreach (T item in newItems)
				Items.Add (item);

			OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
		}

		public void AddRange (IEnumerable<T> newItems)
		{
			if (newItems is null)
				throw new ArgumentNullException (nameof (newItems));

			int index = Items.Count;
			foreach (T item in newItems)
				Items.Add (item);

			OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, newItems.ToList (), index));
		}
	}
}
