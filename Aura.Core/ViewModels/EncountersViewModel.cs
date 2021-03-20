using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Aura.Data;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class EncountersViewModel
		: CampaignElementsViewModel<EncounterElement, EncounterViewModel>
	{
		public EncountersViewModel (IAsyncServiceProvider services)
			: base (services)
		{
		}

		protected override EncounterViewModel InitializeElementViewModel (EncounterElement element)
		{
			return new EncounterViewModel (ServiceProvider, SyncService, element);
		}

		protected override EncounterElement InitializeElement (string name)
		{
			return new EncounterElement {
				Name = name,
				CampaignId = CampaignManager.SelectedElement.Id
			};
		}
	}

	internal class EncounterViewModel
		: ElementViewModel<EncounterElement>
	{
		public EncounterViewModel (IAsyncServiceProvider services, ISyncService sync, EncounterElement element)
			: base (services, sync, element)
		{
			AddStateCommand = new RelayCommand<string> (OnAddStateCommand, s => !String.IsNullOrWhiteSpace (s));
			DeleteStateCommand = new RelayCommand (OnDeleteCommand, SelectedState != null);
			DeleteElementCommand = new RelayCommand (OnRemoveElement, CanRemoveElement);
		}

		public string Name
		{
			get => ModifiedElement.Name;
			set => ModifiedElement = ModifiedElement with { Name = value };
		}

		public EncounterState SelectedState
		{
			get => this.selectedState;
			set
			{
				if (this.selectedState == value)
					return;

				if (value == null) {
					value = ModifiedElement.States.FirstOrDefault ();
				} else if (!ModifiedElement.States.Contains (value)) {
					var states = new List<EncounterState> (ModifiedElement.States);
					int index = states.IndexOf (this.selectedState);
					if (index == -1)
						states.Add (value);
					else
						states[index] = value;

					ModifiedElement = ModifiedElement with { States = states };
				}

				this.selectedState = value;
				RaisePropertyChanged ();

				if (DeleteStateCommand != null) // base loading
					((RelayCommand)DeleteStateCommand).RaiseCanExecuteChanged ();

				UpdateElements ();
			}
		}

		public IReadOnlyList<EncounterState> States => ModifiedElement.States;

		public IReadOnlyList<EncounterStateElementViewModel> Elements => this.elements;

		public ICommand AddStateCommand
		{
			get;
		}

		public ICommand DeleteStateCommand
		{
			get;
		}

		public ICommand DeleteElementCommand
		{
			get;
		}

		public string ElementSearch
		{
			get => this.elementSearch;
			set
			{
				if (this.elementSearch == value)
					return;

				this.elementSearch = value;
				RaisePropertyChanged ();
				SearchElements ();
			}
		}

		public EncounterStateElementViewModel SelectedElement
		{
			get => this.selectedElement;
			set
			{
				if (this.selectedElement == value)
					return;

				this.selectedElement = value;
				RaisePropertyChanged ();
				((RelayCommand)DeleteElementCommand).RaiseCanExecuteChanged ();
			}
		}

		public IReadOnlyList<EnvironmentElementViewModel> ElementSearchResults => this.elementSearchResults;

		public void AddElements (IEnumerable<EnvironmentElementViewModel> newElements)
		{
			if (newElements is null)
				throw new ArgumentNullException (nameof (newElements));

			var elements = new List<EncounterStateElement> (SelectedState.EnvironmentElements);
			elements.AddRange (newElements.Select (eevm => new EncounterStateElement { ElementId = eevm.Id }));

			SelectedState = SelectedState with { EnvironmentElements = elements };
		}

		protected override void OnDelete ()
		{
			IsPreviewing = false;
			base.OnDelete ();
		}

		protected override void OnModified ()
		{
			string selectedElementId = this.selectedElement?.Element.ElementId;

			base.OnModified ();
			RaisePropertyChanged (nameof (Name));
			RaisePropertyChanged (nameof (States));
			if (ModifiedElement == null || !ModifiedElement.States.Contains (SelectedState))
				SelectedState = ModifiedElement?.States.FirstOrDefault ();
			else
				UpdateElements ();

			SelectedElement = (selectedElementId != null)
				? this.elements.FirstOrDefault (evm => evm.Element.ElementId == selectedElementId)
				: null;
		}

		protected override async Task LoadAsync ()
		{
			await base.LoadAsync ();
			SelectedState = States.FirstOrDefault ();
		}

		private string elementSearch;
		private EncounterState selectedState;
		private EncounterStateElementViewModel selectedElement;
		private readonly ObservableCollectionEx<EncounterStateElementViewModel> elements = new ObservableCollectionEx<EncounterStateElementViewModel> ();
		private readonly ObservableCollectionEx<EnvironmentElementViewModel> elementSearchResults = new ObservableCollectionEx<EnvironmentElementViewModel> ();

		private void UpdateElements()
		{
			IEnumerable<EncounterStateElement> elements = SelectedState != null
					? SelectedState.EnvironmentElements
					: Enumerable.Empty<EncounterStateElement> ();

			this.elements.Update (elements, vm => vm.Element, e => new EncounterStateElementViewModel (ServiceProvider, SyncService, this, e));
		}

		private async void SearchElements()
		{
			var results = await SyncService.FindElementsAsync<EnvironmentElement> (ElementSearch);
			this.elementSearchResults.Update (
				results.Select (e => e.Id).Where (s => !Elements.Any (e => e.Element.ElementId == s)),
				eevm => eevm.Id,
				id => new EnvironmentElementViewModel (ServiceProvider, SyncService, results.Single (ee => ee.Id == id)));
		}

		private void OnRemoveElement()
		{
			var elements = new List<EncounterStateElement> (SelectedState.EnvironmentElements);
			elements.Remove (SelectedElement.Element);

			SelectedElement = null;
			SelectedState = SelectedState with { EnvironmentElements = elements };
		}

		private bool CanRemoveElement () => SelectedElement != null;

		private void OnDeleteCommand ()
		{
			var states = new List<EncounterState> (ModifiedElement.States);
			states.Remove (SelectedState);
			ModifiedElement = ModifiedElement with {
				States = states
			};
		}

		private void OnAddStateCommand (string state)
		{
			var encounterState = new EncounterState {
				Name = state
			};

			var states = new List<EncounterState> (ModifiedElement.States);
			states.Add (encounterState);

			ModifiedElement = ModifiedElement with {
				States = states
			};

			SelectedState = encounterState;
		}
	}
}
