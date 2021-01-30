using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;

using Aura.Messages;

namespace Aura.Data
{
	public abstract class JsonSyncServiceBase
		: ISyncService
	{
		protected JsonSyncServiceBase()
		{
			Load ();
		}

		public async Task<T> GetElementByIdAsync<T> (string id) where T : Element
		{
			await Sync.WaitAsync ();
			try {
				if (!this.elements.TryGetValue (typeof (T).GetSimpleTypeName(), out var items))
					return null;

				if (!items.TryGetValue (id, out object value))
					return null;

				return (T)value;
			} finally {
				Sync.Release ();
			}
		}

		public Task<IReadOnlyList<NamedElement>> FindElementsByNameAsync (string search)
		{
			return Task.Run<IReadOnlyList<NamedElement>> (async () => {
				Type nameable = typeof (NamedElement);

				await Sync.WaitAsync ();
				try {
					var found = new List<NamedElement> ();
					foreach (var kvp in this.elements) {
						if (!nameable.IsAssignableFrom (Type.GetType (kvp.Key)))
							continue;

						foreach (NamedElement e in kvp.Value.Values) {
							if (e.Name.IndexOf (search, StringComparison.CurrentCultureIgnoreCase) != -1)
								found.Add (e);
						}
					}

					return found;
				} finally {
					Sync.Release ();
				}
			});
		}

		public async Task<IReadOnlyList<T>> GetElementsAsync<T> () where T : Element
		{
			await Sync.WaitAsync ();
			try {
				if (!this.elements.TryGetValue (typeof (T).GetSimpleTypeName(), out var items))
					return Array.Empty<T> ();

				return items.Values.Cast<T> ().ToList ();
			} finally {
				Sync.Release ();
			}
		}

		public async Task<T> SaveElementAsync<T> (T element)
			where T : Element
		{
			if (element is null)
				throw new ArgumentNullException (nameof (element));

			Type elementType = element.GetType ();

			await Sync.WaitAsync ();
			try {
				if (!this.elements.TryGetValue (elementType.GetSimpleTypeName(), out var items)) {
					this.elements[elementType.GetSimpleTypeName()] = items = new Dictionary<string, object> ();
				}

				string newId = null;
				if (element.Id != null && items.TryGetValue (element.Id, out object existing) && existing is T t) {
					if (t.Version != element.Version) {
						throw new InvalidOperationException ($"Attempted to updat against version {element.Version} but found existing version {t.Version} for {typeof (T)}");
					}
				} else {
					newId = Guid.NewGuid ().ToString ();
				}

				element = (T)element.Update (newId);
				
				items[element.Id] = element;
				await SaveAsync ();
				Messenger.Default.Send (new ElementsChangedMessage (elementType));
				return element;
			} finally {
				Sync.Release ();
			}
		}

		public async Task DeleteElementAsync (Element element)
		{
			if (element is null)
				throw new ArgumentNullException (nameof (element));

			Type elementType = element.GetType ();

			await Sync.WaitAsync ();
			try {
				if (!this.elements.TryGetValue (elementType.GetSimpleTypeName(), out var items)) {
					return;
				}

				if (items.Remove (element.Id)) {
					await SaveAsync ();
					Messenger.Default.Send (new ElementsChangedMessage (elementType));
				}
			} finally {
				Sync.Release ();
			}
		}

		protected SemaphoreSlim Sync
		{
			get;
		} = new SemaphoreSlim (1);

		protected Task SaveAsync()
		{
			return SaveAsync (this.elements);
		}

		protected abstract Task SaveAsync (IDictionary<string, IDictionary<string, object>> data);
		protected abstract Task<IDictionary<string, IDictionary<string, object>>> LoadAsync ();

		private IDictionary<string, IDictionary<string, object>> elements;

		private async void Load()
		{
			this.elements = await LoadAsync ();
		}
	}
}
