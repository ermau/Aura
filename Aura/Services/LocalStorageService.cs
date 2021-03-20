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
				StorageFile file = await GetFileAsync (id, contentHash).ConfigureAwait (false);
				return await file.OpenStreamForReadAsync ().ConfigureAwait (false);
			} catch (FileNotFoundException) {
				return null;
			}
		}

		public async Task<Stream> TryGetStream (Uri fileUri, string contentHash = null)
		{
			if (fileUri is null)
				throw new ArgumentNullException (nameof (fileUri));
			if (!fileUri.IsFile)
				throw new ArgumentException ($"{nameof(fileUri)} does not point to a file", nameof (fileUri));

			try {
				StorageFile file = await StorageFile.GetFileFromPathAsync (fileUri.LocalPath).ConfigureAwait (false);
				return await file.OpenStreamForReadAsync ().ConfigureAwait (false);
			} catch (FileNotFoundException) {
				return null;
			}
		}

		public async Task<StorageFile> GetFileAsync (string id, string contentHash = null)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));

			try {
				return await Storage.GetFileAsync (id).ConfigureAwait (false);
			} catch (FileNotFoundException) {
				return null;
			}
		}

		public async Task<bool> GetIsPresentAsync (string id, string contentHash = null)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));

			try {
				StorageFile file = await Storage.GetFileAsync (id).ConfigureAwait (false);
				// todo: check hash
				return file.IsAvailable;
			} catch (FileNotFoundException) {
				return false;
			}
		}

		public async Task<bool> GetIsPresentAsync (Uri uri)
		{
			if (uri is null)
				throw new ArgumentNullException (nameof (uri));
			if (!uri.IsFile)
				return false;

			try {
				StorageFile file = await StorageFile.GetFileFromPathAsync (uri.LocalPath);
				return file.IsAvailable;
			} catch (FileNotFoundException) {
				return false;
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
