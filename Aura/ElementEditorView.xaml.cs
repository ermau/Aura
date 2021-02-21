using System.Linq;
using Aura.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Aura
{
	public sealed partial class ElementEditorView
		: UserControl
	{
		public ElementEditorView ()
		{
			InitializeComponent ();
		}

		private void OnAudioAddSample (object sender, RoutedEventArgs e)
		{
			var vm = (EnvironmentElementViewModel)DataContext;

			Button button = (Button)sender;
			ListView list = (ListView)button.CommandParameter;
			vm.AudioSamples.AddRange (list.SelectedItems.Cast<SampleViewModel> ());
			vm.AudioSearch = null;

			this.addAudioFlyout.Hide ();
		}
	}
}
