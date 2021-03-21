using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;
using Aura.Messages;

using GalaSoft.MvvmLight.Messaging;

namespace Aura
{
	[Export (typeof(DownloadManager)), Shared]
	internal class DownloadManager
	{
		public DownloadManager (IAsyncServiceProvider services)
		{
			this.services = services ?? throw new ArgumentNullException (nameof (services));
			this.setupTask = SetupAsync ();
		}

		public event EventHandler DownloadsChanged;

		public IReadOnlyList<ManagedDownload> Downloads
		{
			get
			{
				ManagedDownload[] snapshot;
				lock (this.downloads)
					snapshot = this.downloads.ToArray ();

				return snapshot;
			}
		}

		public async Task EnsurePresentAsync (FileSample sample, IProgress<double> progress = null, CancellationToken cancellationToken = default)
		{
			if (sample is null)
				throw new ArgumentNullException (nameof (sample));

			if (await this.storage.GetIsPresentAsync (sample.Id, sample.ContentHash)) {
				progress?.Report (1);
				return;
			}

			if (!this.settings.DownloadInBackground) {
				Task<bool> downloadQuestionTask;
				if (this.downloadInBackground != null)
					downloadQuestionTask = this.downloadInBackground;
				else {
					lock (this.settings) {
						if (this.downloadInBackground == null) {
							// TODO: Localize
							var prompt = new PromptMessage ("Download missing files", "Some of the files for these elements are missing. Would you like to download them now?", "Download");
							Messenger.Default.Send (prompt);
							this.downloadInBackground = prompt.Result;
							downloadQuestionTask = this.downloadInBackground;
						} else
							downloadQuestionTask = this.downloadInBackground;
					}
				}

				if (!await downloadQuestionTask) {
					progress?.Report (1);
					return;
				}
			}

			IContentProviderService[] providers = await this.services.GetServicesAsync<IContentProviderService> ().ConfigureAwait (false);
			IContentProviderService handler = providers.FirstOrDefault (c => c.CanAcquire (sample));

			ManagedDownload download = null;
			if (handler != null) {
				string id = handler.GetEntryIdFromUrl (sample.SourceUrl);
				if (id != null) {
					ContentEntry entry = await handler.GetEntryAsync (id, cancellationToken);
					download = QueueDownload (sample.Id, sample.Name, handler.DownloadEntryAsync (id), entry.Size, sample.ContentHash, progress, cancellationToken);
				}
			}

			if (download == null) {
				if (!Uri.TryCreate (sample.SourceUrl, UriKind.Absolute, out Uri uri)) {
					progress?.Report (1);
					throw new ArgumentException();
				}

				if (uri.IsFile) {
					if (await this.storage.GetIsPresentAsync (uri)) {
						progress?.Report (1);
						return;
					} else {
						progress?.Report (1);
						throw new FileNotFoundException ();
					}
				}

				download = QueueDownload (sample.Id, sample.Name, new Uri (sample.SourceUrl), sample.ContentHash, progress, cancellationToken);
			}

			await download.Task;
		}

		public ManagedDownload QueueImport (string name, Func<CancellationToken, IProgress<double>, Task<FileSample>> getImportTask)
		{
			if (string.IsNullOrWhiteSpace (name))
				throw new ArgumentException ($"'{nameof (name)}' cannot be null or whitespace.", nameof (name));
			if (getImportTask is null)
				throw new ArgumentNullException (nameof (getImportTask));

			var source = new CancellationTokenSource ();

			var download = new ManagedDownload (name);
			download.Task = SampleToHashTask (getImportTask (source.Token, download));
			lock (this.downloads)
				this.downloads.Add (download);

			DownloadsChanged?.Invoke (this, EventArgs.Empty);
			return download;
		}

		public ManagedDownload QueueDownload (string id, string name, Uri uri, string contentHash = null, IProgress<double> progress = null, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace (name))
				throw new ArgumentException ($"'{nameof (name)}' cannot be null or whitespace", nameof (name));
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));
			if (uri is null)
				throw new ArgumentNullException (nameof (uri));

			var download = new ManagedDownload (id, name);
			download.Task = DownloadCoreAsync (download, uri, contentHash, cancellationToken);
			lock (this.downloads)
				this.downloads.Add (download);

			DownloadsChanged?.Invoke (this, EventArgs.Empty);
			return download;
		}

		public ManagedDownload QueueDownload (string id, string name, Task<Stream> stream, long length, string contentHash = null, IProgress<double> progress = null, CancellationToken cancellation = default)
		{
			if (string.IsNullOrWhiteSpace (name))
				throw new ArgumentException ($"'{nameof (name)}' cannot be null or whitespace", nameof (name));
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));
			if (stream is null)
				throw new ArgumentNullException (nameof (stream));

			var download = new ManagedDownload (id, name);
			download.Task = DownloadCoreAsync (download, stream, length, contentHash, cancellation);
			lock (this.downloads)
				this.downloads.Add (download);

			DownloadsChanged?.Invoke (this, EventArgs.Empty);
			return download;
		}

		private readonly List<ManagedDownload> downloads = new List<ManagedDownload> ();
		private readonly IAsyncServiceProvider services;
		private ILocalStorageService storage;
		private SettingsManager settings;
		private Task<bool> downloadInBackground;
		private IContentProviderService[] contentProviders;
		private readonly Task setupTask;

		private async Task SetupAsync()
		{
			this.storage = await this.services.GetServiceAsync<ILocalStorageService> ();
			this.contentProviders = (await this.services.GetServicesAsync<IContentProviderService> ()).ToArray ();
			this.settings = await this.services.GetServiceAsync<SettingsManager> ();
		}

		private async Task<string> SampleToHashTask (Task<FileSample> importTask)
		{
			FileSample sample = null;
			try {
				sample = await importTask;
			} catch {
			}

			return sample?.ContentHash;
		}

		private async Task<string> DownloadCoreAsync (ManagedDownload download, Uri uri, string contentHash, CancellationToken cancellation)
		{
			Task<Stream> stream;
			long? len;

			HttpClient client = new HttpClient();
			try {
				HttpResponseMessage result = await client.GetAsync (uri).ConfigureAwait (false);
				len = result.Content.Headers.ContentLength;
				stream = result.Content.ReadAsStreamAsync ();
			} catch (Exception ex) {
				download.State = DownloadState.DownloadError;
				Trace.WriteLine ("Error downloading: " + ex);
				return null;
			}

			return await DownloadCoreAsync (download, stream, len, contentHash, cancellation);
		}

		private async Task<string> DownloadCoreAsync (ManagedDownload download, Task<Stream> stream, long? length, string contentHash, CancellationToken cancellation)
		{
			await this.setupTask.ConfigureAwait (false);

			Stream writeStream;
			try {
				writeStream = await this.storage.GetWriteStreamAsync (download.ContentId, contentHash).ConfigureAwait (false);
			} catch (Exception ex) {
				Trace.WriteLine ("Error opening write stream: " + ex);
				download.State = DownloadState.LocalError;
				return null;
			}

			Stream readStream;
			try {
				readStream = await stream.ConfigureAwait (false);
			} catch (Exception ex) {
				Trace.WriteLine ("Error downloading: " + ex);
				download.State = DownloadState.DownloadError;
				return null;
			}

			ConcurrentQueue<(byte[] buffer, int len)> chunks = new ();

			var errorCancel = new CancellationTokenSource ();
			Task read = DownloadAsync (chunks, download, readStream, errorCancel, cancellation);
			Task write = WriteAsync (chunks, length, download, writeStream, errorCancel, cancellation);

			try {
				await Task.WhenAll (read, write);
			} catch (AggregateException aex) {
				if (aex.InnerException is OperationCanceledException)
					return null;

				throw;
			}
			
			try {
				SHA256 hasher = SHA256.Create ();
				byte[] hash;
				using (Stream localReadStream = await this.storage.TryGetStream (download.ContentId).ConfigureAwait (false))
					hash = hasher.ComputeHash (localReadStream);

				if (download.State == DownloadState.InProgress)
					download.State = DownloadState.Completed;

				return BitConverter.ToString (hash).Replace ("-", String.Empty);
			} catch (Exception ex) {
				Trace.WriteLine ("Error hashing: " + ex);
				download.State = DownloadState.LocalError;
				return null;
			}
		}

		private async Task DownloadAsync (ConcurrentQueue<(byte[], int)> chunks, ManagedDownload download, Stream readStream, CancellationTokenSource source, CancellationToken cancellation)
		{
			using (readStream) {
				while (download.State == DownloadState.InProgress) {
					var buffer = ArrayPool<byte>.Shared.Rent (8192);

					try {
						int len = await readStream.ReadAsync (buffer, 0, buffer.Length, cancellation).ConfigureAwait (false);
						if (len == 0) {
							chunks.Enqueue ((null, 0));
							break;
						}

						chunks.Enqueue ((buffer, len));
					} catch (OperationCanceledException) {
						download.State = DownloadState.Canceled;
						return;
					} catch (Exception ex) {
						Trace.WriteLine ("Error downloading: " + ex);
						download.State = DownloadState.DownloadError;
						source.Cancel ();
						return;
					}
				}
			}
		}

		private async Task WriteAsync (ConcurrentQueue<(byte[], int)> chunks, long? length, ManagedDownload download, Stream writeStream, CancellationTokenSource source, CancellationToken cancellation)
		{
			if (length == null)
				download.Progress = -1;

			int count = 0;
			using (writeStream) {
				while (!source.IsCancellationRequested) {
					while (chunks.TryDequeue (out var chunk)) {
						if (chunk.Item2 == 0) {
							download.Progress = 1;
							return;
						}

						try {
							await writeStream.WriteAsync (chunk.Item1, 0, chunk.Item2, cancellation).ConfigureAwait (false);
							count += chunk.Item2;
						} catch (OperationCanceledException) {
							download.State = DownloadState.Canceled;
							return;
						} catch (Exception ex) {
							Trace.WriteLine ("Error saving: " + ex);
							download.State = DownloadState.LocalError;
							source.Cancel ();
							return;
						} finally {
							ArrayPool<byte>.Shared.Return (chunk.Item1);
						}

						if (length != null)
							download.Progress = (count / (double)length);
					}

					await Task.Delay (10, source.Token);
				}

				await writeStream.FlushAsync (cancellation).ConfigureAwait (false);
			}
		}
	}

	internal class ManagedDownload
		: NotifyingObject, IProgress<double>
	{
		internal ManagedDownload (string id, string name)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace.", nameof (id));
			if (string.IsNullOrWhiteSpace (name))
				throw new ArgumentException ($"'{nameof (name)}' cannot be null or whitespace", nameof (name));

			ContentId = id;
			Name = name;
		}

		internal ManagedDownload (string name)
		{
			if (string.IsNullOrWhiteSpace (name))
				throw new ArgumentException ($"'{nameof (name)}' cannot be null or whitespace.", nameof (name));

			Name = name;
		}

		public string ContentId
		{
			get;
		}

		public string Name
		{
			get;
		}

		public double Progress
		{
			get => this.progress;
			set
			{
				if (this.progress == value)
					return;

				this.progress = value;
				OnPropertyChanged ();
			}
		}

		public DownloadState State
		{
			get => this.state;
			set
			{
				if (this.state == value)
					return;

				this.state = value;
				OnPropertyChanged ();
			}
		}

		/// <summary>
		/// Gets the task for the actual download resulting in a SHA256 of the file.
		/// </summary>
		public Task<string> Task
		{
			get;
			internal set;
		}

		void IProgress<double>.Report (double value)
		{
			Progress = value;
		}

		private DownloadState state = DownloadState.InProgress;
		private double progress;
	}

	public enum DownloadState
	{
		Unknown = 0,

		InProgress = 1,
		Completed = 2,
		Canceled = 3,

		DownloadError = 10,
		LocalError = 11,
	}
}
