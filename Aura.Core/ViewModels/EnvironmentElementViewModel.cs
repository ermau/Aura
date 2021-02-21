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
			this.generalTiming = new TimingViewModel (this);
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

		public IList<SampleViewModel> AudioSamples => this.audioSamples;

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

		public IReadOnlyList<SampleViewModel> AudioSearchResults => this.audioSearchSamples;

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

		protected override void OnModified ()
		{
			base.OnModified ();
			RaisePropertyChanged (nameof (Name));
			this.generalTiming?.NotifyUpdates ();
			RaisePropertyChanged (nameof (IsFixedPosition));

			RaisePropertyChanged (nameof (AudioRepeat));
			RaisePropertyChanged (nameof (AudioShuffle));

			if (Audio?.Playlist.Samples != null)
				this.audioSamples.Update (Audio.Playlist.Samples, svm => svm.Id, id => new SampleViewModel (ServiceProvider, SyncService, id));
			else
				this.audioSamples.Clear ();
		}

		protected override async Task LoadAsync ()
		{
			await base.LoadAsync ();

			this.loadingSamples = true;
			if (Element.Audio?.Playlist.Samples != null) {
				this.audioSamples.Update (
					Element.Audio?.Playlist.Samples,
					svm => svm.Id,
					id => new SampleViewModel (ServiceProvider, SyncService, id));
			} else
				this.audioSamples.Clear ();
			
			this.loadingSamples = false;
		}

		private readonly TimingViewModel generalTiming, audioTiming, lightingTiming;
		private bool loadingSamples;
		private readonly ObservableCollectionEx<SampleViewModel> audioSamples = new ObservableCollectionEx<SampleViewModel> ();
		
		private readonly ObservableCollectionEx<SampleViewModel> audioSearchSamples = new ObservableCollectionEx<SampleViewModel> ();
		private string audioSearch;

		private void OnAudioSamplesChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			if (this.loadingSamples)
				return;

			Audio = Audio with {
				Playlist = Audio.Playlist with {
					Samples = this.audioSamples.Select (svm => svm.Element.Id).ToArray ()
				}
			};
		}

		private async void SearchAudio()
		{
			IEnumerable<FileSample> results;
			if (!String.IsNullOrWhiteSpace (AudioSearch))
				results = (await SyncService.FindElementsAsync<FileSample> (AudioSearch)).OfType<FileSample> ();
			else
				results = Enumerable.Empty<FileSample> ();

			this.audioSearchSamples.Reset (results
				.Where (fs => !AudioSamples.Any (svm => svm.Element.Id == fs.Id))
				.Select (fs => new SampleViewModel (ServiceProvider, SyncService, fs)));
		}

		private class TimingViewModel
			: ViewModelBase
		{
			public TimingViewModel (EnvironmentElementViewModel parent)
			{
				this.parent = parent;
			}

			public TimeSpan MinStartDelay
			{
				get => ModifiedElement.Timing.MinStartDelay;
				set => ModifiedElement = ModifiedElement with
				{
					Timing = ModifiedElement.Timing with
					{
						MinStartDelay = value
					}
				};
			}

			public TimeSpan MaxStartDelay
			{
				get => ModifiedElement.Timing.MaxStartDelay;
				set => ModifiedElement = ModifiedElement with
				{
					Timing = ModifiedElement.Timing with
					{
						MaxStartDelay = value
					}
				};
			}

			public TimeSpan MinimumReoccurance
			{
				get => ModifiedElement.Timing.MinimumReoccurance;
				set => ModifiedElement = ModifiedElement with
				{
					Timing = ModifiedElement.Timing with
					{
						MinimumReoccurance = value
					}
				};
			}

			public TimeSpan MaximumReoccurance
			{
				get => ModifiedElement.Timing.MaximumReoccurance;
				set => ModifiedElement = ModifiedElement with
				{
					Timing = ModifiedElement.Timing with
					{
						MaximumReoccurance = value
					}
				};
			}

			public void NotifyUpdates()
			{
				RaisePropertyChanged (nameof (MinStartDelay));
				RaisePropertyChanged (nameof (MaxStartDelay));
				RaisePropertyChanged (nameof (MinimumReoccurance));
				RaisePropertyChanged (nameof (MaximumReoccurance));
			}

			protected EnvironmentElement ModifiedElement
			{
				get => this.parent.ModifiedElement;
				set => this.parent.ModifiedElement = value;
			}

			private readonly EnvironmentElementViewModel parent;
		}
	}
}
