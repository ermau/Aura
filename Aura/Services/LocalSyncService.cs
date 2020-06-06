using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Aura.Messages;

using GalaSoft.MvvmLight.Messaging;

using Newtonsoft.Json;
using Windows.Storage;

namespace Aura.Services
{
	[Export (typeof(ISyncService)), Shared]
	internal class LocalSyncService
		: ISyncService
	{
		public LocalSyncService()
		{
			LoadAsync ();
		}

		public async Task<T> GetElementByIdAsync<T> (string id) where T : Element
		{
			await this.sync.WaitAsync ();
			try {
				if (!this.elements.TryGetValue (GetSimpleTypeName (typeof (T)), out var items))
					return null;

				if (!items.TryGetValue (id, out object value))
					return null;

				return (T)value;
			} finally {
				this.sync.Release ();
			}
		}

		public Task<IReadOnlyList<NamedElement>> FindElementsByNameAsync (string search)
		{
			return Task.Run<IReadOnlyList<NamedElement>> (async () => {
				Type nameable = typeof (NamedElement);

				await this.sync.WaitAsync ();
				try {
					var found = new List<NamedElement> ();
					foreach (var kvp in this.elements) {
						if (!nameable.IsAssignableFrom (Type.GetType (kvp.Key)))
							continue;

						foreach (NamedElement e in kvp.Value.Values) {
							if (e.Name.Contains (search, StringComparison.CurrentCultureIgnoreCase))
								found.Add (e);
						}
					}

					return found;
				} finally {
					this.sync.Release ();
				}
			});
		}

		public async Task<IReadOnlyList<T>> GetElementsAsync<T> () where T : Element
		{
			await this.sync.WaitAsync ();
			try {
				if (!this.elements.TryGetValue (GetSimpleTypeName (typeof (T)), out var items))
					return Array.Empty<T> ();

				return items.Values.Cast<T>().ToList ();
			} finally {
				this.sync.Release ();
			}
		}

		public async Task SaveElementAsync (Element element)
		{
			if (element is null)
				throw new ArgumentNullException (nameof (element));

			Type elementType = element.GetType ();

			await this.sync.WaitAsync ().ConfigureAwait (false);
			try {
				if (!this.elements.TryGetValue (GetSimpleTypeName (elementType), out var items)) {
					this.elements[GetSimpleTypeName (elementType)] = items = new Dictionary<string, object> ();
				}

				element.Id = element.Id ?? Guid.NewGuid ().ToString ();
				items[element.Id] = element;
				await SaveAsync();
				Messenger.Default.Send (new ElementsChangedMessage (elementType));
			} finally {
				this.sync.Release ();
			}
		}

		public async Task DeleteElementAsync (Element element)
		{
			if (element is null)
				throw new ArgumentNullException (nameof (element));

			Type elementType = element.GetType ();

			await this.sync.WaitAsync ().ConfigureAwait (false);
			try {
				if (!this.elements.TryGetValue (GetSimpleTypeName (elementType), out var items)) {
					return;
				}

				if (items.Remove (element.Id)) {
					await SaveAsync ().ConfigureAwait (false);
					Messenger.Default.Send (new ElementsChangedMessage (elementType));
				}
			} finally {
				this.sync.Release ();
			}
		}

		protected virtual StorageFolder RoamingFolder
		{
			get { return ApplicationData.Current.RoamingFolder; }
		}

		private readonly SemaphoreSlim sync = new SemaphoreSlim (1);
		private Dictionary<string, Dictionary<string, object>> elements;

		private const string DbFilename = "db.json";

		private Task LoadAsync()
		{
			this.sync.Wait ();
			return Task.Run (async () => {
				try {
					StorageFile db = await RoamingFolder.GetFileAsync (DbFilename);
					Stream stream = (await db.OpenReadAsync ()).AsStreamForRead();

					var serializer = new JsonSerializer {
						TypeNameHandling = TypeNameHandling.Auto
					};

					this.elements = (Dictionary<string, Dictionary<string, object>>)serializer.Deserialize (new StreamReader (stream), typeof(Dictionary<string, Dictionary<string, object>>));
				} catch (FileNotFoundException) {
					this.elements = new Dictionary<string, Dictionary<string, object>> ();
				} finally {
					this.sync.Release ();
				}
			});
		}

		private async Task SaveAsync()
		{
			Task<string> serialize = Task.Run (() => {
				return JsonConvert.SerializeObject (this.elements, new JsonSerializerSettings {
					TypeNameHandling = TypeNameHandling.Auto,
					Formatting = Formatting.Indented
				});
			});

			StorageFile db = await RoamingFolder.CreateFileAsync (DbFilename, CreationCollisionOption.ReplaceExisting);
			using (var stream = await db.OpenStreamForWriteAsync ())
			using (var writer = new StreamWriter (stream)) {
				await writer.WriteAsync (await serialize);
			}
		}

		private string GetSimpleTypeName (object instance) => GetSimpleTypeName (instance?.GetType ());

		private string GetSimpleTypeName (Type type)
		{
			if (type == null)
				return null;

			return $"{type.FullName}, {type.Assembly.GetName ().Name}";
		}
	}
}
