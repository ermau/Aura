using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Aura
{
	internal static class Importer
	{
		static Importer()
		{
			SupportedFiles = new HashSet<string> (AudioExtensions);
		}

		public static ICollection<string> SupportedFiles
		{
			get;
		}
			
		public static ICollection<string> AudioExtensions = new HashSet<string> {
			".wav",
			".mp3",
			".ogg",
			".m4a",
			".flac",
			".aac",
			".wma"
		};

		public static async Task<FileSample> ImportAsync (IStorageFile file, ISyncService sync, IProgress<double> progress = null, CancellationToken cancelToken = default)
		{
			FileSample sample = GetSampleBase (file);
			if (sample == null) {
				Trace.TraceWarning ("Could not import file " + file.Path);
				progress?.Report (1);
				return null;
			}

			var total = (progress != null) ? new AggregateProgress (progress) : null;
			var localProgress = total?.CreateProgressNode ();
			var scanProgress = total?.CreateProgressNode ();
			total?.FinishDiscovery ();

			try {
				ISampledService service = await App.Services.GetServiceAsync (sample).ConfigureAwait (false);

				string token = StorageApplicationPermissions.FutureAccessList.Add (file);
				sample = sample with { Token = token };
				sample = await service.ScanSampleAsync (sample, scanProgress).ConfigureAwait (false);
				localProgress?.Report (.5);

				var existing = (await sync.FindElementsAsync<FileSample> (fs => fs.ContentHash == sample.ContentHash || fs.SourceUrl == sample.SourceUrl).ConfigureAwait (false)).FirstOrDefault ();
				if (existing != null) {
					if (existing.ContentHash != sample.ContentHash) {
						// We'll use the newly scanned sample and update the record, the file changed since the hash does not match
						StorageApplicationPermissions.FutureAccessList.Remove (existing.Token);
						sample = sample with
						{
							Id = existing.Id,
							Tags = existing.Tags,
							Events = existing.Events,
							Version = existing.Version
						};
					} else {
						return existing;
					}
				}

				localProgress?.Report (.75);

				cancelToken.ThrowIfCancellationRequested ();
				if (sample != null)
					sample = await sync.SaveElementAsync (sample).ConfigureAwait (false);

				return sample;
			} finally {
				localProgress?.Report (1);
				scanProgress?.Report (1);
			}
		}

		public static async Task ImportAsync (IReadOnlyList<IStorageItem> items, IProgress<double> progress = null)
		{
			items = GetImportableItems (items);

			AggregateProgress total = (progress != null) ? new AggregateProgress (progress) : null;

			ISyncService sync = await App.Services.GetServiceAsync<ISyncService> ().ConfigureAwait (false);
			DownloadManager downloads = await App.Services.GetServiceAsync<DownloadManager> ().ConfigureAwait (false);

			List<Task> importTasks = new List<Task> ();
			foreach (IStorageItem item in items) {
				var node = total?.CreateProgressNode ();
				if (item is IStorageFolder folder) {
					var children = await folder.GetItemsAsync ();
					importTasks.Add (ImportAsync (children, node));
				} else if (item is IStorageFile file) {
					Task<FileSample> import = null;
					ManagedDownload download = null;

					Func<CancellationToken, IProgress<double>, Task<FileSample>> importGetter = async (cancel, progress) => {
						import = ImportAsync (file, sync, progress, cancel);
						FileSample sample = null;
						try {
							sample = await import;
							if (download != null)
								download.State = DownloadState.Completed;
						} catch {
							if (download != null)
								download.State = DownloadState.LocalError;
						}

						return sample;
					};
					download = downloads.QueueImport (file.Name, importGetter);
					importTasks.Add (import);
				}
			}

			total?.FinishDiscovery ();
			await Task.WhenAll (importTasks);
		}

		private static IReadOnlyList<IStorageItem> GetImportableItems (IReadOnlyList<IStorageItem> items)
		{
			var storageItems = new List<IStorageItem> (items.Count);
			foreach (IStorageItem item in items) {
				if (item is IStorageFolder)
					storageItems.Add (item);
				else if (item is IStorageFile file) {
					string ext = Path.GetExtension (file.Name);
					if (SupportedFiles.Contains (ext))
						storageItems.Add (item);
				}
			}

			return storageItems;
		}

		private static FileSample GetSampleBase (IStorageFile file)
		{
			// TODO: We could move this to the environment service to actually attempt to load the file
			string ext = Path.GetExtension (file.Name);
			if (String.IsNullOrWhiteSpace (ext))
				return null;

			FileSample baseSample = null;
			if (AudioExtensions.Contains (ext)) {
				baseSample = new AudioSample ();
			}

			if (baseSample != null) {
				baseSample = baseSample with
				{
					SourceUrl = file.Path,
					Name = Path.GetFileNameWithoutExtension (file.Name)
				};
			}

			return baseSample;
		}
	}
}
