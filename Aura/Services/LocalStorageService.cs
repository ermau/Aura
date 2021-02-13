using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Aura.Services
{
	[Export (typeof (ILocalStorageService))]
	internal class LocalStorageService
		: ILocalStorageService
	{
		public async Task<Stream> TryGetStream (string id, string contentHash = null)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));

			try {
				StorageFile file = await Storage.GetFileAsync (id).ConfigureAwait (false);
				return await file.OpenStreamForReadAsync ().ConfigureAwait (false);
			} catch (FileNotFoundException) {
				return null;
			}
		}

		public async Task<Stream> GetWriteStreamAsync (string id, string contentHash = null)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));

			// TODO: We can check for its existence against content hash later
			StorageFile file = await Storage.CreateFileAsync (id, CreationCollisionOption.ReplaceExisting).ConfigureAwait (false);
			return await file.OpenStreamForWriteAsync ().ConfigureAwait (false);
		}

		public async Task DeleteAsync (string id, string contentHash = null)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));

			try {
				StorageFile file = await Storage.GetFileAsync (id).ConfigureAwait (false);
				await file.DeleteAsync (StorageDeleteOption.PermanentDelete);
			} catch (FileNotFoundException) {
			}
		}

		private StorageFolder Storage
		{
			get { return ApplicationData.Current.LocalFolder; }
		}
	}
}
