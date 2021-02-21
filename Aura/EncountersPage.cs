using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aura.ViewModels;

using Windows.UI.Xaml.Navigation;

namespace Aura
{
	public class EncountersPage
		: MasterDetailPage
	{
		public EncountersPage ()
		{
			Title = "Encounters";
			ShowExport = true;
			DataContext = new EncountersViewModel (App.Services);
			PaneContent = new EncounterEditorView ();
		}

		protected override void OnNavigatedTo (NavigationEventArgs e)
		{
			base.OnNavigatedTo (e);

			if (e.Parameter is string id) {
				((EncountersViewModel)DataContext).RequestSelection (id);
			}
		}
	}
}
