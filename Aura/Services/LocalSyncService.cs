using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Data;
using Aura.Messages;

using GalaSoft.MvvmLight.Messaging;

using Newtonsoft.Json;
using Windows.Storage;

namespace Aura.Services
{
	[Export (typeof(ISyncService)), Shared]
	internal class LocalSyncService
		: JsonSyncServiceBase, ISyncService
	{
		protected virtual StorageFolder RoamingFolder
		{
			get { return ApplicationData.Current.RoamingFolder; }
		}

		private const string DbFilename = "db.json";

		protected override Task<IDictionary<string, IDictionary<string, object>>> LoadAsync()
		{
			Sync.Wait ();
			return Task.Run (async () => {
				Stream stream = null;

				try {
					StorageFile db = await RoamingFolder.GetFileAsync (DbFilename);
					stream = (await db.OpenReadAsync ()).AsStreamForRead ();

					var serializer = new JsonSerializer {
						TypeNameHandling = TypeNameHandling.Auto
					};

					return (IDictionary<string, IDictionary<string, object>>)serializer.Deserialize (new StreamReader (stream), typeof (IDictionary<string, IDictionary<string, object>>));
				} catch (FileNotFoundException) {
					return new Dictionary<string, IDictionary<string, object>> ();
				} catch (JsonSerializationException) {
					if (stream != null)
						stream.Dispose ();

					await RoamingFolder.RenameAsync (DbFilename + ".corrupt");
					await SaveAsync ();

					return new Dictionary<string, IDictionary<string, object>> ();
				} finally {
					Sync.Release ();
				}
			});
		}

		protected override async Task SaveAsync(IDictionary<string, IDictionary<string, object>> data)
		{
			Task<string> serialize = Task.Run (() => {
				return JsonConvert.SerializeObject (data, new JsonSerializerSettings {
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
	}
}
