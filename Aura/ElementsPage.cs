using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aura.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace Aura
{
	public class ElementsPage
		: MasterDetailPage
	{
		public ElementsPage ()
		{
			Title = "Elements";
			DataContext = new EnvironmentElementsViewModel (App.Services);
			PaneContent = new ElementEditorView ();
		}

		protected override void OnNavigatedTo (NavigationEventArgs e)
		{
			base.OnNavigatedTo (e);

			if (e.Parameter is string id) {
				((EnvironmentElementsViewModel)DataContext).RequestSelection (id);
			}
		}
	}
}
