using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aura.ViewModels;

namespace Aura
{
	public class ElementsPage
		: MasterDetailPage
	{
		public ElementsPage ()
		{
			Title = "Elements";
			DataContext = new EnvironmentElementsViewModel (App.Services);
		}
	}
}
