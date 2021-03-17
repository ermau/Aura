using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Aura.Data;

using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class RunEncounterStateViewModel
		: DataViewModel
	{
		public RunEncounterStateViewModel (IAsyncServiceProvider serviceProvider, EncounterState state)
			: base (serviceProvider)
		{
			State = state ?? throw new ArgumentNullException (nameof (state));
			PlayCommand = new RelayCommand (OnPlay, CanPlay);
			StopCommand = new RelayCommand (OnStop, CanStop);
			Load ();
		}

		public ICommand PlayCommand
		{
			get;
		}

		public ICommand StopCommand
		{
			get;
		}

		public IReadOnlyList<RunEnvironmentElementViewModel> Elements
		{
			get;
			private set;
		}

		public EncounterState State
		{
			get;
		}

		public bool IsPlaying
		{
			get => this.isPlaying;
			private set
			{
				if (this.isPlaying == value)
					return;

				this.isPlaying = value;
				RaisePropertyChanged ();
			}
		}

		public void Prepare()
		{
			this.prepareTask = this.playback.PrepareEncounterStateAsync (State);
		}

		public override void Cleanup ()
		{
			base.Cleanup ();
			this.playback.EnvironmentChanged -= OnPlayingEnvironmentChanged;
		}

		protected override async Task SetupAsync ()
		{
			await base.SetupAsync ();
			this.playback = await ServiceProvider.GetServiceAsync<PlaybackManager> ();
			this.playback.EnvironmentChanged += OnPlayingEnvironmentChanged;
		}

		private bool isPlaying;
		private PlaybackManager playback;
		private Task<PlaybackEnvironment> prepareTask;

		private async void Load()
		{
			AddWork ();
			try {
				await LoadAsync ();
			} finally {
				FinishWork ();
			}
		}

		private async Task LoadAsync()
		{
			await SetupTask;

			Elements = State.EnvironmentElements.Select (e =>
				new RunEnvironmentElementViewModel (ServiceProvider, SyncService, e)).ToArray();
			RaisePropertyChanged (nameof (Elements));
		}

		private async void OnPlayingEnvironmentChanged (object sender, PlaybackEnvironmentChangedEventArgs e)
		{
			if (this.prepareTask == null)
				return;

			PlaybackEnvironment environment = await this.prepareTask;
			IsPlaying = environment == e.NewEnvironment;
		}

		private bool CanPlay ()
		{
			return true;
		}

		private async void OnPlay ()
		{
			Task<PlaybackEnvironment> task = this.prepareTask ?? (this.prepareTask = this.playback.PrepareEncounterStateAsync (State));

			PlaybackEnvironment environment = await task;
			this.playback.PlayEnvironment (environment);
			((RelayCommand)StopCommand).RaiseCanExecuteChanged ();
		}

		private bool CanStop() => this.prepareTask != null;

		private void OnStop()
		{
			this.playback.Stop ();
		}
	}
}