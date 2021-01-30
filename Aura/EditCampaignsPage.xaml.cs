
using Aura.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Aura
{
	public sealed partial class EditCampaignsPage : Page
	{
		public EditCampaignsPage ()
		{
			InitializeComponent ();
			DataContext = new EditCampaignsViewModel (App.Services);
		}
	}
}
