using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;

using Aura.Messages;
using System.Linq.Expressions;

namespace Aura.Data
{
	public abstract class JsonSyncServiceBase
		: ISyncService
	{
		protected JsonSyncServiceBase()
		{
			Load ();
		}

		public async Task<Element> GetElementByIdAsync (string id)
		{
			await this.loadTask;
			await Sync.WaitAsync ();
			try {
				foreach (var items in this.elements.Values) {
					if (items.TryGetValue (id, out object value))
						return (Element)value;
				}

				return null;
			} finally {
				Sync.Release ();
			}
		}

		public async Task<T> GetElementByIdAsync<T> (string id) where T : Element
		{
			await this.loadTask;
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

				var found = new List<NamedElement> ();
				await this.loadTask;
				await Sync.WaitAsync ();
				try {
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

		public Task<IReadOnlyList<T>> FindElementsAsync<T> (Expression<Func<T, bool>> predicateExpression) where T : Element
		{
			if (predicateExpression is null)
				throw new ArgumentNullException (nameof (predicateExpression));

			var predicateTask = Task.Run (() => predicateExpression.Compile ());
			return Task.Run (async () => {
				Type type = typeof (T);
				var found = new List<T> ();
				await this.loadTask;
				await Sync.WaitAsync ();
				try {
					foreach (var kvp in this.elements) {
						if (!type.IsAssignableFrom (Type.GetType (kvp.Key)))
							continue;

						foreach (T e in kvp.Value.Values) {
							if (predicateTask.Result (e))
								found.Add (e);
						}
					}

					return (IReadOnlyList<T>)found;
				} finally {
					Sync.Release ();
				}
			});
		}

		public async Task<IReadOnlyList<T>> GetElementsAsync<T> () where T : Element
		{
			await this.loadTask;
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

			await this.loadTask;

			await Sync.WaitAsync ();
			try {
				if (!this.elements.TryGetValue (elementType.GetSimpleTypeName(), out var items)) {
					this.elements[elementType.GetSimpleTypeName()] = items = new Dictionary<string, object> ();
				}

				if (element.Id == null) {
					element = (T)element.Update (Guid.NewGuid().ToString());
				} else if (items.TryGetValue (element.Id, out object existing) && existing is T t) {
					if (t.Version != element.Version) {
						throw new InvalidOperationException ($"Attempted to update against version {element.Version} but found existing version {t.Version} for {typeof (T)}");
					}
				}
				
				items[element.Id] = element;
				await SaveAsync ();
			} finally {
				Sync.Release ();
			}

			Messenger.Default.Send (new ElementsChangedMessage (elementType, element.Id));
			return element;
		}

		public async Task DeleteElementAsync (Element element)
		{
			if (element is null)
				throw new ArgumentNullException (nameof (element));

			Type elementType = element.GetType ();

			await this.loadTask;

			await Sync.WaitAsync ();
			try {
				if (!this.elements.TryGetValue (elementType.GetSimpleTypeName(), out var items)) {
					return;
				}

				if (items.Remove (element.Id)) {
					await SaveAsync ();
					Messenger.Default.Send (new ElementsChangedMessage (elementType, element.Id));
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
		private Task loadTask;

		private async Task LoadCoreAsync()
		{
			var results = await LoadAsync ();
			this.elements = results;
		}

		private async void Load()
		{
			var load = LoadCoreAsync ();
			this.loadTask = load;
			await load;
		}
	}
}
