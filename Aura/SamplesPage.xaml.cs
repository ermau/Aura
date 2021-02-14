using Aura.ViewModels;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Aura
{
	public sealed partial class SamplesPage : Page
	{
		public SamplesPage ()
		{
			DataContext = new SamplesViewModel (App.Services);
			InitializeComponent ();
		}

		protected override void OnNavigatedTo (NavigationEventArgs e)
		{
			base.OnNavigatedTo (e);

			if (e.Parameter is string id) {
				((SamplesViewModel)DataContext).RequestSelection (id);
			}
		}

		protected void OnAddClick (object sender, RoutedEventArgs e)
		{
			Frame.Navigate (typeof (ImportSamplePage));
		}
	}
}
