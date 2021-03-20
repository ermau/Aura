using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

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

		public async Task UpdateAsync<TId> (IEnumerable<TId> newItems, Func<T, TId> basedOn, Func<TId, Task<T>> getElementAsync)
		{
			if (newItems is null)
				throw new ArgumentNullException (nameof (newItems));
			if (basedOn is null)
				throw new ArgumentNullException (nameof (basedOn));
			if (getElementAsync is null)
				throw new ArgumentNullException (nameof (getElementAsync));

			var newList = newItems.ToList ();

			var existingMap = this.ToDictionary (basedOn);

			foreach (var kvp in existingMap) {
				if (!newList.Contains (kvp.Key))
					Remove (kvp.Value);
			}

			int i;
			for (i = 0; i < newList.Count; i++) {
				TId newId = newList[i];

				var elements = await Task.WhenAll (newList.Skip (i).Select (getElementAsync));
				if (i < Items.Count) {
					T existing = Items[i];
					if (!Equals (basedOn (existing), newId)) {
						T element = await getElementAsync (newId);
						if (element != null)
							Insert (i, element);
					}
				} else {
					AddRange (elements.Where (e => e != null));
					break;
				}
			}
		}

		public void Update<TId> (IEnumerable<TId> newItems, Func<T, TId> basedOn, Func<TId, T> getElement)
		{
			if (newItems is null)
				throw new ArgumentNullException (nameof (newItems));
			if (basedOn is null)
				throw new ArgumentNullException (nameof (basedOn));
			if (getElement is null)
				throw new ArgumentNullException (nameof (getElement));

			var newList = newItems.ToList ();

			var existingMap = this.ToDictionary (basedOn);

			foreach (var kvp in existingMap) {
				if (!newList.Contains (kvp.Key))
					Remove (kvp.Value);
			}

			int i;
			for (i = 0; i < newList.Count; i++) {
				TId newId = newList[i];

				if (i < Items.Count) {
					T existing = Items[i];
					if (!Equals (basedOn (existing), newId))
						Insert (i, getElement (newId));
				} else {
					AddRange (newList.Skip (i).Select (getElement));
					break;
				}
			}
		}

		public void Update<TId> (IEnumerable<T> newItems, Func<T, TId> basedOn)
		{
			if (newItems is null)
				throw new ArgumentNullException (nameof (newItems));
			if (basedOn is null)
				throw new ArgumentNullException (nameof (basedOn));

			var newList = newItems.ToList ();

			var existingMap = this.ToDictionary (basedOn);

			foreach (var kvp in existingMap) {
				int idx = newList.FindIndex (e => Equals (basedOn (e), kvp.Key));
				if (idx == -1) {
					if (existingMap.TryGetValue (kvp.Key, out T value)) {
						idx = IndexOf (value);
						Items.RemoveAt (idx);
					}
				}
			}

			int i;
			for (i = 0; i < newList.Count; i++) {
				T newElement = newList[i];

				if (i < Items.Count) {
					T existing = Items[i];
					if (!Equals (basedOn (existing), basedOn (newElement)))
						Insert (i, newElement);
				} else {
					AddRange (newList.Skip (i));
					break;
				}
			}
		}

		public void AddRange (IEnumerable<T> newItems)
		{
			if (newItems is null)
				throw new ArgumentNullException (nameof (newItems));

			var items = newItems.ToList ();
			if (items.Count == 0)
				return;

			int index = Items.Count;
			foreach (T item in items) {
				Add (item);
				//Items.Add (item);
			}

			//OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, items, index));
		}

		private int FindIndex<TId> (Func<T, TId> basedOn, TId id)
		{
			for (int i = 0; i < Items.Count; i++) {
				TId elementId = basedOn (Items[i]);
				if (Equals (elementId, id))
					return i;
			}

			return -1;
		}
	}
}
