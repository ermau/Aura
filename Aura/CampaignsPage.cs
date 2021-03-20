
using Aura.ViewModels;

namespace Aura
{
	internal class CampaignsPage
		: MasterDetailPage
	{
		public CampaignsPage()
		{
			DataContext = new EditCampaignsViewModel (App.Services);
			Title = "Campaigns";
			ShowSorting = false;
			ShowExport = true;
		}
	}
}
