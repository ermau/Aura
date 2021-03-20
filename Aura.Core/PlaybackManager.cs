//
// PlaybackManager.cs
//
// Authors:
//       Eric Maupin <me@ermau.com>
//
// Copyright (c) 2021 Eric Maupin
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura
{
	internal class PlaybackEnvironmentChangedEventArgs
		: EventArgs
	{
		public PlaybackEnvironmentChangedEventArgs (PlaybackEnvironment previousEnvironment, PlaybackEnvironment newEnvironment)
		{
			PreviousEnvironment = previousEnvironment;
			NewEnvironment = newEnvironment;
		}

		public PlaybackEnvironment PreviousEnvironment { get; }
		public PlaybackEnvironment NewEnvironment { get; }
	}

	[Export (typeof(PlaybackManager))]
	internal class PlaybackManager
	{
		public PlaybackManager (IAsyncServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider ?? throw new ArgumentNullException (nameof (serviceProvider));
			this.loadedServices = LoadServicesAsync ();

			new Thread (TimelineRunner).Start ();
		}

		public TimeSpan TransitionTime
		{
			get;
			set;
		} = TimeSpan.FromSeconds (2);

		public event EventHandler<PlaybackEnvironmentChangedEventArgs> EnvironmentChanged;

		public async Task<IReadOnlyList<FileSample>> EnsureElementPresentAsync (EnvironmentElement element, IProgress<double> progress = null, CancellationToken cancellationToken = default)
		{
			if (element is null)
				throw new ArgumentNullException (nameof (element));

			await this.loadedServices.ConfigureAwait (false);

			List<Task> tasks = new List<Task> ();
			List<FileSample> samples = new List<FileSample> ();

			AggregateProgress totalProgress = (progress != null) ? new AggregateProgress (progress) : null;
			if (element.Audio != null) {
				foreach (string sampleId in element.Audio.Playlist.Descriptors) {
					FileSample sample = await this.sync.GetElementByIdAsync<FileSample> (sampleId).ConfigureAwait (false);
					if (sample == null)
						continue;

					IProgress<double> nodeProgress = totalProgress?.CreateProgressNode ();

					samples.Add (sample);
					tasks.Add (this.downloadManager.EnsurePresentAsync (sample, nodeProgress, cancellationToken));
				}
			}

			totalProgress?.FinishDiscovery ();

			int offset = 0;
			for (int i = 0; i < tasks.Count; i++) {
				try {
					await tasks[i].ConfigureAwait(false);
				} catch {
					samples.RemoveAt (i - offset++);
				}
			}

			return samples;
		}

		public async Task<PlaybackEnvironment> PrepareEncounterStateAsync (EncounterState encounterState, IProgress<double> progress = null, CancellationToken cancellationToken = default)
		{
			if (encounterState is null)
				throw new ArgumentNullException (nameof (encounterState));

			await this.loadedServices.ConfigureAwait (false);

			AggregateProgress totalProgress = (progress != null) ? new AggregateProgress (progress) : null;

			var elements = new List<(EncounterStateElement, EnvironmentElement)> ();
			var presentTasks = new List<Task<IReadOnlyList<FileSample>>> ();
			foreach (EncounterStateElement stateElement in encounterState.EnvironmentElements) {
				EnvironmentElement element = await this.sync.GetElementByIdAsync<EnvironmentElement> (stateElement.ElementId).ConfigureAwait (false);
				elements.Add ((stateElement, element));
				presentTasks.Add (EnsureElementPresentAsync (element, totalProgress?.CreateProgressNode(), cancellationToken));
			}

			// Lower level errors should be handled by EnsurePresent.
			// If EnsurePresent fails we should bubble up because we can't be sure we can prepare the encounter.
			await Task.WhenAll (presentTasks).ConfigureAwait (false);

			var playbackElements = new List<PlaybackEnvironmentElement> (elements.Count);
			for (int i = 0; i < elements.Count; i++) {
				(var state, var element) = elements[i];
				playbackElements.Add (new PlaybackEnvironmentElement (state, element));//, presentTasks[i].Result));
			}

			return new PlaybackEnvironment (playbackElements);
		}

		public void PlayEnvironment (PlaybackEnvironment environment, bool transition = true)
		{
			if (environment is null)
				throw new ArgumentNullException (nameof (environment));

			PlaybackEnvironment previous;
			this.lck.Wait ();
			try {
				previous = Interlocked.Exchange (ref this.currentEnvironment, environment);
				if (previous != null) {
					var originalPrevious = Interlocked.Exchange (ref this.previousEnvironment, previous);
					originalPrevious?.Stop ();

					if (transition) {
						previous.TransitionToAsync (environment, TransitionTime);
					} else {
						previous.Stop ();
					}
				} else if (transition) {
					environment.FadeInAsync (TransitionTime);
				}
			} finally {
				this.lck.Release ();
			}

			EnvironmentChanged?.Invoke (this, new PlaybackEnvironmentChangedEventArgs (previous, environment));
		}

		/// <summary>
		/// Stops the <paramref name="environment"/> if it is the currently playing environment.
		/// </summary>
		public void StopEnvironment (PlaybackEnvironment environment, bool transition = true)
		{
			if (this.currentEnvironment != environment)
				return;

			PlaybackEnvironment previous;
			this.lck.Wait ();
			try {
				if (this.currentEnvironment != environment)
					return;

				previous = Interlocked.Exchange (ref this.currentEnvironment, null);

				var originalPrevious = Interlocked.Exchange (ref this.previousEnvironment, previous);
				originalPrevious?.Stop ();
			} finally {
				this.lck.Release ();
			}

			if (previous == null)
				return;

			if (transition) {
				previous.FadeOutAsync (TransitionTime);
			} else {
				previous.Stop ();
			}

			EnvironmentChanged?.Invoke (this, new PlaybackEnvironmentChangedEventArgs (previous, null));
		}

		/// <summary>
		/// Stops the currently playing environment.
		/// </summary>
		public void Stop (bool transition = true)
		{
			StopEnvironment (this.currentEnvironment, transition);
		}

		private readonly SemaphoreSlim lck = new SemaphoreSlim (1);
		private readonly IAsyncServiceProvider serviceProvider;
		private IReadOnlyList<IEnvironmentService> enabledServices;
		private Task<IEnvironmentService[]> availableServices;
		private Task loadedServices;

		private PlaybackEnvironment currentEnvironment, previousEnvironment;

		private PlaySpaceManager playSpaceManager;
		private DownloadManager downloadManager;
		private ILocalStorageService storage;
		private ISyncService sync;
		private bool servicesUpdated = true;

		private void TimelineRunner (object state)
		{
			this.loadedServices.Wait ();

			ActiveServices services = null;
			while (true) {
				PlaybackEnvironment environment = this.currentEnvironment;
				PlaybackEnvironment previous = this.previousEnvironment;
				
				if (this.servicesUpdated) {
					this.lck.Wait ();
					var enabled = this.enabledServices.ToArray ();
					services = new ActiveServices {
						Audio = enabled.OfType<IAudioService>().FirstOrDefault(),
						Lighting = enabled.OfType<ILightingService>().FirstOrDefault(),
						Storage = this.storage
					};
					this.lck.Release ();
				}

				environment?.Tick (services);
				previous?.Tick (services);

				Thread.Sleep (1);
			}
		}

		private async Task LoadServicesAsync()
		{
			this.availableServices = this.serviceProvider.GetServicesAsync<IEnvironmentService> ();
			
			this.sync = await this.serviceProvider.GetServiceAsync<ISyncService> ().ConfigureAwait (false);
			this.playSpaceManager = await this.serviceProvider.GetServiceAsync<PlaySpaceManager> ().ConfigureAwait (false);
			this.playSpaceManager.PropertyChanged += OnPlayspaceManagerPropertyChanged;
			
			this.downloadManager = await this.serviceProvider.GetServiceAsync<DownloadManager> ().ConfigureAwait (false);
			this.storage = await this.serviceProvider.GetServiceAsync<ILocalStorageService> ().ConfigureAwait (false);

			await (this.loadedServices = UpdateServicesAsync ());
		}

		private void OnPlayspaceManagerPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (PlaySpaceManager.SelectedElement))
				UpdateServices ();
		}

		private async void UpdateServices()
		{
			await (this.loadedServices = UpdateServicesAsync ());
		}

		private async Task UpdateServicesAsync()
		{
			List<Task> tasks = new();
			await this.lck.WaitAsync().ConfigureAwait (false);
			this.servicesUpdated = true;
			try {
				var oldServices = this.enabledServices ?? Enumerable.Empty<IEnvironmentService>();
				this.enabledServices = this.playSpaceManager.GetServices (await this.availableServices.ConfigureAwait (false)).ToArray ();

				var removed = new HashSet<IEnvironmentService> (oldServices).Except (this.enabledServices);
				foreach (IEnvironmentService service in removed)
					tasks.Add (service.StopAsync ());

				var added = new HashSet<IEnvironmentService> (this.enabledServices).Except (oldServices);
				foreach (IEnvironmentService service in added)
					tasks.Add (service.StartAsync (this.serviceProvider));
			} finally {
				this.lck.Release ();
			}

			await Task.WhenAll (tasks);
		}
	}
}
