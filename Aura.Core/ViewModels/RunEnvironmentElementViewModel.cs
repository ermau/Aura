using System;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura.ViewModels
{
	internal class RunEnvironmentElementViewModel
		: EnvironmentElementViewModel
	{
		public RunEnvironmentElementViewModel(IAsyncServiceProvider serviceProvider, ISyncService syncService, EncounterStateElement element)
			: base (serviceProvider, syncService, element.ElementId)
		{
			StateElement = element ?? throw new ArgumentNullException (nameof (element));
			Load ();
		}

		public EncounterStateElement StateElement
		{
			get;
		}

		public EnvironmentElement EnvironmentElement => Element;

		protected override async Task LoadAsync ()
		{
			await base.LoadAsync ();
			RaisePropertyChanged (nameof (EnvironmentElement));
		}
	}
}