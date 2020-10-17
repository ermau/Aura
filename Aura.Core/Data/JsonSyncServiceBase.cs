using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Messages;
using GalaSoft.MvvmLight.Messaging;

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
				if (!this.elements.TryGetValue (GetSimpleTypeName (typeof (T)), out var items))
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
							if (e.Name.Contains (search))
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
				if (!this.elements.TryGetValue (GetSimpleTypeName (typeof (T)), out var items))
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
				if (!this.elements.TryGetValue (GetSimpleTypeName (elementType), out var items)) {
					this.elements[GetSimpleTypeName (elementType)] = items = new Dictionary<string, object> ();
				}

				element.Id = element.Id ?? Guid.NewGuid ().ToString ();
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
				if (!this.elements.TryGetValue (GetSimpleTypeName (elementType), out var items)) {
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

		private string GetSimpleTypeName (Type type)
		{
			if (type == null)
				return null;

			return $"{type.FullName}, {type.Assembly.GetName ().Name}";
		}
	}
}
