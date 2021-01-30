using Aura.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Aura
{
	public sealed partial class CreateCampaignDialog
		: ContentDialog
	{
		public CreateCampaignDialog ()
		{
			DataContext = new CreateCampaignDialogViewModel (App.Services);
			InitializeComponent ();
		}
	}
}
