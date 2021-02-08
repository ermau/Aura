using System;
using System.Threading.Tasks;

using Aura.ViewModels;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Aura
{
	public sealed partial class WaitForPairDialog
		: ContentDialog
	{
		public WaitForPairDialog ()
		{
			InitializeComponent ();
			DataContextChanged += OnDataContextChanged;
		}

		private void OnDataContextChanged (FrameworkElement sender, DataContextChangedEventArgs args)
		{
			if (args.NewValue is PairServiceViewModel vm) {
				CloseOnTask (vm.PairResult);
			}
		}

		private async void CloseOnTask (Task task)
		{
			try {
				await task;
			} catch (OperationCanceledException) {
			} finally {
				Hide ();
			}
		}
	}
}
