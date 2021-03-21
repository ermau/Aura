using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura.ViewModels
{
	internal class RunGameViewModel
		: DataViewModel
	{
		public RunGameViewModel (IAsyncServiceProvider serviceProvider)
			: base (serviceProvider)
		{
		}

		public IReadOnlyList<EncounterElement> Encounters
		{
			get => this.encouters;
			private set
			{
				this.encouters = value;
				RaisePropertyChanged ();
			}
		}

		public EncounterElement SelectedEncounter
		{
			get => this.selectedEncounter;
			set
			{
				if (this.selectedEncounter == value)
					return;

				this.selectedEncounter = value;
				RaisePropertyChanged ();
				ReloadStates ();
			}
		}

		public IReadOnlyList<RunEncounterStateViewModel> EncounterStates
		{
			get => this.states;
			private set
			{
				this.states = value;
				RaisePropertyChanged ();
			}
		}

		public RunEncounterStateViewModel SelectedState
		{
			get => this.selectedState;
			set
			{
				if (this.selectedState == value)
					return;

				this.selectedState = value;
				RaisePropertyChanged ();
			}
		}

		public override void Cleanup ()
		{
			base.Cleanup ();
			this.campaigns.PropertyChanged -= OnCampainsPropertyChanged;
		}

		protected override async Task SetupAsync ()
		{
			await base.SetupAsync ();
			this.campaigns = await ServiceProvider.GetServiceAsync<CampaignManager> ();
			this.campaigns.PropertyChanged += OnCampainsPropertyChanged;
			OnCampaignChanged ();
		}

		private CampaignManager campaigns;
		private IReadOnlyList<EncounterElement> encouters;
		private EncounterElement selectedEncounter;
		private IReadOnlyList<RunEncounterStateViewModel> states;
		private RunEncounterStateViewModel selectedState;

		private void OnCampainsPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (CampaignManager.SelectedElement))
				OnCampaignChanged ();
		}

		private void ReloadStates()
		{
			EncounterElement encounter = SelectedEncounter;
			if (encounter == null) {
				EncounterStates = Array.Empty<RunEncounterStateViewModel> ();
				SelectedState = null;
				return;
			}

			EncounterStates = encounter.States.Select (s => new RunEncounterStateViewModel (ServiceProvider, s)).ToArray ();
			SelectedState = EncounterStates.FirstOrDefault ();
		}

		private async void ReloadEncounters()
		{
			AddWork ();
			try {
				await LoadEncountersAsync ();
			} finally {
				FinishWork ();
			}
		}

		private async Task LoadEncountersAsync()
		{
			string campaignId = this.campaigns.SelectedElement.Id;
			if (campaignId == null) {
				Encounters = null;
				return;
			}

			Encounters = (await SyncService.GetElementsAsync<EncounterElement> ())
				.Where (ee => ee.CampaignId == campaignId).ToArray();
			SelectedEncounter = Encounters.FirstOrDefault ();
		}

		private void OnCampaignChanged ()
		{
			ReloadEncounters ();
		}
	}
}