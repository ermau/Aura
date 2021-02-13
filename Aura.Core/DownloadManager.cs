using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aura
{
	[Export (typeof(DownloadManager)), Shared]
	internal class DownloadManager
	{
		[ImportingConstructor]
		public DownloadManager (ILocalStorageService storage)
		{
			this.storage = storage ?? throw new ArgumentNullException (nameof (storage));
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

		public ManagedDownload QueueDownload (string id, string name, Task<Stream> stream, long length, string contentHash = null)
		{
			if (string.IsNullOrWhiteSpace (name))
				throw new ArgumentException ($"'{nameof (name)}' cannot be null or whitespace", nameof (name));
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));
			if (stream is null)
				throw new ArgumentNullException (nameof (stream));

			var download = new ManagedDownload (id, name);
			download.DownloadTask = DownloadCoreAsync (download, stream, length, contentHash);
			lock (this.downloads)
				this.downloads.Add (download);

			DownloadsChanged?.Invoke (this, EventArgs.Empty);
			return download;
		}

		private readonly ILocalStorageService storage;
		private readonly List<ManagedDownload> downloads = new List<ManagedDownload> ();

		private async Task<string> DownloadCoreAsync (ManagedDownload download, Task<Stream> stream, long length, string contentHash)
		{
			Stream writeStream;
			try {
				writeStream = await this.storage.GetWriteStreamAsync (download.ContentId, contentHash);
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
			Task read = DownloadAsync (chunks, download, readStream, errorCancel);
			Task write = WriteAsync (chunks, length, download, writeStream, errorCancel);

			await Task.WhenAll (read, write);
			
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

		private async Task DownloadAsync (ConcurrentQueue<(byte[], int)> chunks, ManagedDownload download, Stream readStream, CancellationTokenSource source)
		{
			using (readStream) {
				while (download.State == DownloadState.InProgress) {
					var buffer = ArrayPool<byte>.Shared.Rent (8192);

					try {
						int len = await readStream.ReadAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
						if (len == 0)
							break;

						chunks.Enqueue ((buffer, len));
					} catch (Exception ex) {
						Trace.WriteLine ("Error downloading: " + ex);
						download.State = DownloadState.DownloadError;
						source.Cancel ();
						return;
					}
				}
			}
		}

		private async Task WriteAsync (ConcurrentQueue<(byte[], int)> chunks, long length, ManagedDownload download, Stream writeStream, CancellationTokenSource source)
		{
			int count = 0;
			using (writeStream) {
				while (!source.IsCancellationRequested && count < length) {
					while (chunks.TryDequeue (out var chunk)) {
						try {
							await writeStream.WriteAsync (chunk.Item1, 0, chunk.Item2).ConfigureAwait (false);
							count += chunk.Item2;
						} catch (Exception ex) {
							Trace.WriteLine ("Error saving: " + ex);
							download.State = DownloadState.LocalError;
							source.Cancel ();
							return;
						} finally {
							ArrayPool<byte>.Shared.Return (chunk.Item1);
						}

						download.Progress = (count / (double)length);
					}

					await Task.Delay (10, source.Token);
				}

				await writeStream.FlushAsync ().ConfigureAwait (false);
			}
		}
	}

	internal class ManagedDownload
		: NotifyingObject
	{
		internal ManagedDownload (string id, string name)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));
			if (string.IsNullOrWhiteSpace (name))
				throw new ArgumentException ($"'{nameof (name)}' cannot be null or whitespace", nameof (name));

			ContentId = id;
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
		public Task<string> DownloadTask
		{
			get;
			internal set;
		}

		private DownloadState state = DownloadState.InProgress;
		private double progress;
	}

	public enum DownloadState
	{
		Unknown = 0,

		InProgress = 1,
		Completed = 2,

		DownloadError = 10,
		LocalError = 11,
	}
}
