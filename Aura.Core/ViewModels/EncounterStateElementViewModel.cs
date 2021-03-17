using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura.ViewModels
{
	internal class EncounterStateElementViewModel
		: DataItemViewModel<EncounterStateElement>
	{
		public EncounterStateElementViewModel (IAsyncServiceProvider services, ISyncService sync, EncounterViewModel encounter, EncounterStateElement element)
			: base (services, sync)
		{
			this.encounter = encounter ?? throw new System.ArgumentNullException (nameof (encounter));
			Element = element ?? throw new System.ArgumentNullException (nameof (element));
			ModifiedElement = Element;
			Load ();
		}

		public EnvironmentElement EnvironmentElement
		{
			get => this.element;
			private set
			{
				this.element = value;
				RaisePropertyChanged ();
			}
		}

		public bool StartsWithState
		{
			get => ModifiedElement.StartsWithState;
			set
			{
				ModifiedElement = ModifiedElement with { StartsWithState = value };
			}
		}

		private EnvironmentElement element;
		private EncounterViewModel encounter;

		protected override void OnModified ()
		{
			base.OnModified ();
			RaisePropertyChanged (nameof (StartsWithState));
		}

		protected override async Task LoadAsync ()
		{
			await SetupTask;
			EnvironmentElement = await SyncService.GetElementByIdAsync<EnvironmentElement> (Element.ElementId);
		}

		protected override async Task SaveAsync ()
		{
			EncounterState state = this.encounter.SelectedState;

			await SetupTask;

			var elements = new List<EncounterStateElement> (state.EnvironmentElements);
			int index = elements.IndexOf (Element);
			if (index == -1)
				elements.Add (ModifiedElement);
			else
				elements[index] = ModifiedElement;

			state = state with { EnvironmentElements = elements };

			this.encounter.SelectedState = state;
			this.encounter.SaveCommand.Execute (null);
		}
	}
}
