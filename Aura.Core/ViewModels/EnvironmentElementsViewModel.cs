using Aura.Data;

namespace Aura.ViewModels
{
	internal class EnvironmentElementsViewModel
		: EnvironmentElementsViewModel<EnvironmentElement, EnvironmentElementViewModel<EnvironmentElement>>
	{
		public EnvironmentElementsViewModel (IAsyncServiceProvider services)
			: base (services)
		{
		}

		protected override EnvironmentElement InitializeElement (string name)
		{
			return new EnvironmentElement {
				Name = name
			};
		}

		protected override EnvironmentElementViewModel<EnvironmentElement> CreateElementViewModel (EnvironmentElement element)
		{
			return new EnvironmentElementViewModel<EnvironmentElement> (ServiceProvider, SyncService, element);
		}
	}


	internal abstract class EnvironmentElementsViewModel<T, TViewModel>
		: CampaignElementsViewModel<T, TViewModel>
		where T : EnvironmentElement
		where TViewModel : ElementViewModel<T>
	{
		public EnvironmentElementsViewModel (IAsyncServiceProvider services)
			: base (services)
		{
		}
	}
}
