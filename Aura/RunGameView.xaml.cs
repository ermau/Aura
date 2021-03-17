using Aura.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Aura
{
	public sealed partial class RunGameView
		: UserControl
	{
		public RunGameView ()
		{
			DataContext = new RunGameViewModel (App.Services);
			InitializeComponent ();
		}
	}
}
