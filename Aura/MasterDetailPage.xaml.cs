using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Aura.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Aura
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public partial class MasterDetailPage : Page
	{
		public MasterDetailPage ()
		{
			this.InitializeComponent ();
		}

		public static readonly DependencyProperty TitleProperty =
			DependencyProperty.Register (nameof (Title), typeof (string), typeof (MasterDetailPage), new PropertyMetadata (null));

		public string Title
		{
			get => (string)GetValue (TitleProperty);
			set => SetValue (TitleProperty, value);
		}

		public static readonly DependencyProperty ShowSortingProperty =
			DependencyProperty.Register (nameof (ShowSorting), typeof (bool), typeof (MasterDetailPage), new PropertyMetadata (true));

		public bool ShowSorting
		{
			get => (bool)GetValue (ShowSortingProperty);
			set => SetValue (ShowSortingProperty, value);
		}

		public static readonly DependencyProperty ShowExportProperty =
			DependencyProperty.Register (nameof (ShowExport), typeof (bool), typeof (MasterDetailPage), new PropertyMetadata (false));

		public bool ShowExport
		{
			get => (bool)GetValue (ShowExportProperty);
			set => SetValue (ShowExportProperty, value);
		}

		public static readonly DependencyProperty PaneContentProperty =
			DependencyProperty.Register (nameof (PaneContent), typeof (object), typeof (MasterDetailPage), new PropertyMetadata (null, 
				(d,e) => {
					if (e.OldValue is FrameworkElement fe) {
						fe.ClearValue (DataContextProperty);
					}

					if (e.NewValue is FrameworkElement newFe) {
						newFe.SetBinding (DataContextProperty, new Binding {
							Source = ((MasterDetailPage)d).elementList,
							Path = new PropertyPath (nameof (ListBox.SelectedItem))
						});
					}
				}));

		public object PaneContent
		{
			get => GetValue (PaneContentProperty);
			set => SetValue (PaneContentProperty, value);
		}

		protected void OnAddClick (object sender, RoutedEventArgs e)
		{
			CommitAdd ();
		}

		protected void OnKeyDown (object sender, KeyRoutedEventArgs e)
		{
			if (e.Key != Windows.System.VirtualKey.Enter)
				return;

			CommitAdd ();
		}

		private void CommitAdd()
		{
			if (this.addButton.Command.CanExecute (this.addButton.CommandParameter))
				this.addButton.Command.Execute (this.addButton.CommandParameter);

			this.addElementFlyout.Hide ();
			this.addName.Text = String.Empty;
		}
	}
}
