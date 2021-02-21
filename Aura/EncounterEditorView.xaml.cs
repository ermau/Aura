using System;
using System.Linq;
using Aura.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Aura
{
	public sealed partial class EncounterEditorView : UserControl
	{
		public EncounterEditorView ()
		{
			InitializeComponent ();
		}

		private void OnAddState (object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			CommitAddState ();
		}

		private void AddStateKeyDown (object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (e.Key != Windows.System.VirtualKey.Enter)
				return;

			if (this.addStateButton.Command.CanExecute(this.createName.Text)) {
				this.addStateButton.Command.Execute (this.createName.Text);
				CommitAddState ();
			}
		}

		private void CommitAddState()
		{
			this.createName.Text = String.Empty;
			this.addStateFlyout.Hide ();
		}

		private void CommitAddElement()
		{
			EncounterViewModel vm = (EncounterViewModel)DataContext;
			vm.AddElements (this.elementSearchList.SelectedItems.Cast<EnvironmentElementViewModel> ());

			this.addElementSearch.Text = String.Empty;
			this.addElementFlyout.Hide ();
		}

		private void OnAddElement (object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			CommitAddElement ();
		}

		private void OnElementSearchKeyDown (object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (e.Key != Windows.System.VirtualKey.Enter)
				return;

			CommitAddElement ();
		}
	}
}
