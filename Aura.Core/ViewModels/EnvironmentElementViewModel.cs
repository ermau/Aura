using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Aura.Data;

using GalaSoft.MvvmLight;

namespace Aura.ViewModels
{
	internal class EnvironmentElementViewModel
		: ElementViewModel<EnvironmentElement>
	{
		public EnvironmentElementViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService, EnvironmentElement element)
			: base (serviceProvider, syncService, element)
		{
			this.generalTiming = new TimingViewModel ((timing) => ModifiedElement = ModifiedElement with { Timing = timing }, () => ModifiedElement.Timing);
			this.audioSamples.CollectionChanged += OnAudioSamplesChanged;
		}

		public EnvironmentElementViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService, string id)
			: base (serviceProvider, syncService, id)
		{
			this.generalTiming = new TimingViewModel ((timing) => ModifiedElement = ModifiedElement with { Timing = timing }, () => ModifiedElement.Timing);
			this.audioSamples.CollectionChanged += OnAudioSamplesChanged;
		}

		public string Name
		{
			get => ModifiedElement.Name;
			set => ModifiedElement = ModifiedElement with { Name = value };
		}

		public object GeneralTiming => this.generalTiming;

		public bool IsFixedPosition
		{
			get => ModifiedElement.Positioning.FixedPosition != null;
		}

		public float MinDistance
		{
			get => ModifiedElement.Positioning.MinimumDistance?.X ?? 0;
			set => Positioning = Positioning with { MinimumDistance = new Position { X = value, Y = value } };
		}

		public float MaxDistance
		{
			get => ModifiedElement.Positioning.MaximumDistance?.X ?? 0;
			set => Positioning = Positioning with { MaximumDistance = new Position { X = value, Y = value } };
		}

		public IList<AudioSampleViewModel> AudioSamples => this.audioSamples;

		public bool AudioShuffle
		{
			get => (Audio?.Playlist.Order ?? SourceOrder.InOrder) == SourceOrder.Shuffle;
			set => Audio = Audio with {
				Playlist = Audio.Playlist with {
					Order = (value) ? SourceOrder.Shuffle : SourceOrder.InOrder
				}
			};
		}

		public bool AudioRepeat
		{
			get => (Audio?.Playlist.Repeat ?? SourceRepeatMode.None) == SourceRepeatMode.Continuous;
			set => Audio = Audio with {
				Playlist = Audio.Playlist with {
					Repeat = (value) ? SourceRepeatMode.Continuous : SourceRepeatMode.None
				}
			};
		}

		public string AudioSearch
		{
			get => this.audioSearch;
			set
			{
				if (this.audioSearch == value)
					return;

				this.audioSearch = value;
				RaisePropertyChanged ();
				SearchAudio ();
			}
		}

		public IReadOnlyList<AudioSampleViewModel> AudioSearchResults => this.audioSearchSamples;

		public LightingEffectViewModel Lighting
		{
			get;
		}

		protected AudioComponent Audio
		{
			get => ModifiedElement.Audio ?? new AudioComponent();
			set => ModifiedElement = ModifiedElement with {
				Audio = value
			};
		}

		protected Positioning Positioning
		{
			get => ModifiedElement.Positioning;
			set => ModifiedElement = ModifiedElement with { Positioning = value };
		}

		protected override async void OnModified ()
		{
			await OnModifiedAsync ();
		}

		protected override async Task LoadAsync ()
		{
			await base.LoadAsync ();
			await OnModifiedAsync ();
		}

		private readonly TimingViewModel generalTiming, audioTiming, lightingTiming;
		private bool loadingSamples;
		private readonly ObservableCollectionEx<AudioSampleViewModel> audioSamples = new ObservableCollectionEx<AudioSampleViewModel> ();
		
		private readonly ObservableCollectionEx<AudioSampleViewModel> audioSearchSamples = new ObservableCollectionEx<AudioSampleViewModel> ();
		private string audioSearch;

		private async Task OnModifiedAsync()
		{
			base.OnModified ();
			RaisePropertyChanged (nameof (Name));
			this.generalTiming?.NotifyUpdates ();
			RaisePropertyChanged (nameof (IsFixedPosition));

			RaisePropertyChanged (nameof (AudioRepeat));
			RaisePropertyChanged (nameof (AudioShuffle));

			if (Audio?.Playlist.Descriptors != null) {
				this.loadingSamples = true;
				await this.audioSamples.UpdateAsync (Audio.Playlist.Descriptors.Distinct (), svm => svm.Id, async id => {
					var sample = await SyncService.GetElementByIdAsync<AudioSample> (id);
					if (sample == null)
						return null;

					return new AudioSampleViewModel (ServiceProvider, SyncService, sample);
				});
				this.loadingSamples = false;
			} else
				this.audioSamples.Clear ();
		}

		private void OnAudioSamplesChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			if (this.loadingSamples)
				return;

			Audio = Audio with {
				Playlist = Audio.Playlist with {
					Descriptors = this.audioSamples.Select (svm => svm.Element.Id).Distinct().ToArray ()
				}
			};
		}

		private async void SearchAudio()
		{
			IEnumerable<AudioSample> results;
			if (!String.IsNullOrWhiteSpace (AudioSearch))
				results = (await SyncService.FindElementsAsync<AudioSample> (AudioSearch));
			else
				results = Enumerable.Empty<AudioSample> ();

			this.audioSearchSamples.Reset (results
				.Where (fs => !AudioSamples.Any (svm => svm.Element.Id == fs.Id))
				.Select (fs => new AudioSampleViewModel (ServiceProvider, SyncService, fs)));
		}

		private class TimingViewModel
			: ViewModelBase
		{
			public TimingViewModel (Action<Timing> setTiming, Func<Timing> getTiming)
			{
				this.setTiming = setTiming;
				this.getTiming = getTiming;
			}

			public string MinStartDelay
			{
				get { return Timing.MinStartDelay.ToString (); }
				set
				{
					if (Timing.MinStartDelay.ToString () == value)
						return;

					if (!TimeSpan.TryParse (value, out TimeSpan time))
						return;

					Timing = Timing with { MinStartDelay = time };
					RaisePropertyChanged ();
				}
			}

			public string MaxStartDelay
			{
				get { return Timing.MaxStartDelay.ToString (); }
				set
				{
					if (Timing.MaxStartDelay.ToString () == value)
						return;

					if (!TimeSpan.TryParse (value, out TimeSpan time))
						return;

					Timing = Timing with { MaxStartDelay = time };
					RaisePropertyChanged ();
				}
			}

			public string MinimumReoccurance
			{
				get { return Timing.MinimumReoccurance.ToString (); }
				set
				{
					if (Timing.MinimumReoccurance.ToString () == value)
						return;

					if (!TimeSpan.TryParse (value, out TimeSpan time))
						return;

					Timing = Timing with { MinimumReoccurance = time };
					RaisePropertyChanged ();
				}
			}

			public string MaximumReoccurance
			{
				get { return Timing.MaximumReoccurance.ToString (); }
				set
				{
					if (Timing.MaximumReoccurance.ToString () == value)
						return;

					if (!TimeSpan.TryParse (value, out TimeSpan time))
						return;

					Timing = Timing with { MaximumReoccurance = time };
					RaisePropertyChanged ();
				}
			}

			public void NotifyUpdates()
			{
				RaisePropertyChanged (nameof (MinStartDelay));
				RaisePropertyChanged (nameof (MaxStartDelay));
				RaisePropertyChanged (nameof (MinimumReoccurance));
				RaisePropertyChanged (nameof (MaximumReoccurance));
			}

			protected Timing Timing
			{
				get => this.getTiming ();
				set => this.setTiming (value);
			}

			private readonly Action<Timing> setTiming;
			private readonly Func<Timing> getTiming;
		}
	}
}
