using Windows.UI.Xaml.Controls;

using Aura.ViewModels;

namespace Aura
{
	public sealed partial class SettingsPage
		: Page
	{
		public SettingsPage ()
		{
			DataContext = new SettingsViewModel (App.Services);
			InitializeComponent ();
		}
	}
}
