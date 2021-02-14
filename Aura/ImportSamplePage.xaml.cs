using Aura.ViewModels;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Aura
{
	public sealed partial class ImportSamplePage : Page
	{
		public ImportSamplePage ()
		{
			NavigationCacheMode = NavigationCacheMode.Enabled;
			DataContext = new ContentSearchViewModel (App.Services);
			InitializeComponent ();
		}
	}
}
