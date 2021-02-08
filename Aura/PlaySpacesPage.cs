using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aura.ViewModels;

namespace Aura
{
	public class PlaySpacesPage
		: MasterDetailPage
	{
		public  PlaySpacesPage()
		{
			Title = "Play Spaces";
			ShowSorting = false;
			DataContext = new PlaySpacesViewModel (App.Services);
		}
	}
}
