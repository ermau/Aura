using Aura.Data;

namespace Aura.ViewModels
{
	internal class EnvironmentElementsViewModel
		: EnvironmentElementsViewModel<EnvironmentElement, EnvironmentElementViewModel>
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

		protected override EnvironmentElementViewModel InitializeElementViewModel (EnvironmentElement element)
		{
			return new EnvironmentElementViewModel (ServiceProvider, SyncService, element);
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
