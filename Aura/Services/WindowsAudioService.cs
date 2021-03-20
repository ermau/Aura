//
// WindowsAudioService.cs
//
// Authors:
//       Eric Maupin <me@ermau.com>
//
// Copyright (c) 2020 Eric Maupin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Aura.Services
{
	[Export (typeof(IAudioService))]
	[Export (typeof(IEnvironmentService))]
	[Shared]
	internal class WindowsAudioService
		: IAudioService, IDisposable
	{
		public string DisplayName => "Windows Audio";

		public async Task StartAsync (IAsyncServiceProvider services)
		{
			if (services is null)
				throw new ArgumentNullException (nameof (services));

			this.storage = await services.GetServiceAsync<ILocalStorageService> ();
			this.sync = await services.GetServiceAsync<ISyncService> ();

			if (this.graph == null) {
				var result = await AudioGraph.CreateAsync (new AudioGraphSettings (AudioRenderCategory.GameEffects)).ConfigureAwait (false);
				if (result.Status != AudioGraphCreationStatus.Success)
					throw new InvalidOperationException ("AudioGraph creation failed, " + result.Status);

				this.graph = result.Graph;
			}

			this.graph.Start ();

			if (this.output == null) {
				var outputResult = await this.graph.CreateDeviceOutputNodeAsync ().ConfigureAwait (false);
				if (outputResult.Status != AudioDeviceNodeCreationStatus.Success)
					throw new InvalidOperationException ("Output device node creation failed, " + outputResult.Status);

				this.output = outputResult.DeviceOutputNode;
			}

			this.output.Start ();
		}

		public async Task<FileSample> ScanSampleAsync (FileSample sample, IProgress<double> progress = null)
		{
			if (sample is null)
				throw new ArgumentNullException (nameof (sample));
			if (!(sample is AudioSample audio))
				throw new ArgumentException ("Must be an audio sample");

			try {
				StorageFile file;
				try {
					file = await StorageFile.GetFileFromPathAsync (sample.SourceUrl).ConfigureAwait (false);
				} catch (AccessViolationException) {
					if (sample.Token != null)
						file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync (sample.Token).ConfigureAwait (false);
					else
						throw;
				}

				CreateAudioFileInputNodeResult result = await this.graph.CreateFileInputNodeAsync (file);
				if (result.Status != AudioFileNodeCreationStatus.Success) {
					Trace.TraceWarning ($"Failed to scan file sample: {result.Status}");
					return null;
				}

				using (var node = result.FileInputNode) {
					progress?.Report (.5);

					var hashTask = GetContentHashAsync (await file.OpenStreamForReadAsync ());

					audio = audio with {
						Duration = node.Duration,
						Channels = (AudioChannels)node.EncodingProperties.ChannelCount,
						Frequency = node.EncodingProperties.SampleRate,
						ContentHash = await hashTask
					};
				}

				return audio;
			} catch (Exception ex) {
				Trace.TraceWarning ("Failed to scan file sample: " + ex);
				return null;
			} finally {
				progress?.Report (1);
			}
		}

		public Task AdjustPlaybackAsync (IPreparedEffect prepared, PlaybackOptions options)
		{
			PreparedSource preparedSource = (PreparedSource)prepared;
			AdjustEmitter (preparedSource.Emitter, options);
			return Task.CompletedTask;
		}

		public void PlayEffect (IPreparedEffect element)
		{
			if (this.output == null)
				throw new InvalidOperationException ();

			PreparedSource prepared = element as PreparedSource;
			if (prepared == null)
				throw new ArgumentException (nameof (element));

			if (prepared.Node.OutgoingConnections.Count == 0)
				prepared.Node.AddOutgoingConnection (this.output);
			else
				prepared.Node.Reset ();

			prepared.Node.Start ();
		}

		public void SetIntensity (IPreparedEffect prepared, double intensity)
		{
			if (prepared == null)
				throw new ArgumentNullException (nameof (prepared));

			PreparedSource source = (PreparedSource)prepared;
			source.SetIntensity (intensity);
		}

		public void Dispose ()
		{
			if (this.graph == null)
				return;

			this.output.Stop ();
			this.graph.Stop ();
			this.graph.Dispose ();
			this.output.Dispose ();

			this.graph = null;
		}

		public Task StopAsync()
		{
			this.output.Stop ();
			this.graph.Stop ();
			return Task.CompletedTask;
		}

		public async Task<IPreparedEffect> PrepareEffectAsync (EnvironmentElement element, string descriptor, PlaybackOptions options)
		{
			CreateAudioFileInputNodeResult nodeResult;

			AudioSample sample = await this.sync.GetElementByIdAsync<AudioSample> (descriptor).ConfigureAwait (false);

			StorageFile file = await GetFileAsync (sample).ConfigureAwait (false);

			AudioNodeEmitter emitter = null;// GetEmitter (options);
			if (emitter != null)
				nodeResult = await graph.CreateFileInputNodeAsync (file, emitter).ConfigureAwait (false);
			else
				nodeResult = await graph.CreateFileInputNodeAsync (file).ConfigureAwait (false);

			var node = nodeResult.FileInputNode;
			var prepared = new PreparedSource (node, emitter) { Duration = nodeResult.FileInputNode.Duration };

			/*
			node.FileCompleted += (o, e) => {
				OnElementFinished (new PreparedElementEventArgs (prepared));
			};*/

			return prepared;
		}

		private AudioGraph graph;
		private ILocalStorageService storage;
		private ISyncService sync;

		private AudioDeviceOutputNode output;

		private Task<string> GetContentHashAsync (Stream stream)
		{
			return Task.Run (() => {
				SHA256 hasher = SHA256.Create ();
				byte[] hash;
				using (stream)
					hash = hasher.ComputeHash (stream);

				return BitConverter.ToString (hash).Replace ("-", String.Empty);
			});
		}

		private async Task<StorageFile> GetFileAsync (AudioSample sample)
		{
			if (Uri.TryCreate (sample.SourceUrl, UriKind.Absolute, out Uri uri) && uri.IsFile) {
				StorageFile file = null;
				try {
					file = await StorageFile.GetFileFromPathAsync (sample.SourceUrl).ConfigureAwait (false);
				} catch (AccessViolationException) {
					if (sample.Token != null) {
						try {
							file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync (sample.Token).ConfigureAwait (false);
						} catch {
						}
					}
				}

				if (file != null)
					return file;
			}

			if (this.storage is LocalStorageService localStorage) {
				return await localStorage.GetFileAsync (sample.Id, sample.ContentHash).ConfigureAwait (false);
			}

			using (Stream stream = await storage.TryGetStream (sample.Id, sample.ContentHash).ConfigureAwait (false)) {
				StorageFile streamedFile = await StorageFile.CreateStreamedFileAsync (sample.Id, (r) => {
					stream.CopyTo (r.AsStreamForWrite ());
				}, null).ConfigureAwait (false);

				return streamedFile;
			}
		}

		private void AdjustEmitter (AudioNodeEmitter emitter, PlaybackOptions options)
		{
			var position = options.Position;
			emitter.Position = new System.Numerics.Vector3 (position.X, position.Y, position.Z);
		}

		private AudioNodeEmitter GetEmitter (PlaybackOptions options)
		{
			if (options.Position == null)
				return null;

			var shape = AudioNodeEmitterShape.CreateOmnidirectional ();
			var decay = AudioNodeEmitterDecayModel.CreateNatural (0.1, 1.0, 10, 100);

			var emitter = new AudioNodeEmitter (shape, decay, AudioNodeEmitterSettings.None);
			emitter.SpatialAudioModel = SpatialAudioModel.ObjectBased;
			AdjustEmitter (emitter, options);
			return emitter;
		}

		private class PreparedSource
			: IPreparedEffect, IDisposable
		{
			public PreparedSource (IAudioInputNode node, AudioNodeEmitter emitter)
			{
				Node = node;
				Emitter = emitter;
			}

			public IAudioInputNode Node { get; }
			public AudioNodeEmitter Emitter { get; }

			public TimeSpan Duration
			{
				get;
				set;
			}

			public void SetIntensity (double intensity)
			{
				if (this.disposed)
					return;

				Node.OutgoingGain = intensity;
			}

			public void Dispose ()
			{
				if (this.disposed)
					return;

				this.disposed = true;
				Node.Dispose ();
			}

			private bool disposed;
		}
	}
}
