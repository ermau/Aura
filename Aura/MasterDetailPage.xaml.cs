using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

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
			Popup popup = ((DependencyObject)sender).FindParent<FlyoutPresenter> ()?.Parent as Popup;
			if (popup == null)
				return;

			popup.IsOpen = false;

			TextBox text = popup.FindName ("createName") as TextBox;
			if (text != null) {
				text.Text = null;
			}
		}

		protected void OnKeyDown (object sender, KeyRoutedEventArgs e)
		{
			if (e.Key != Windows.System.VirtualKey.Enter)
				return;

			TextBox text = sender as TextBox;
			string input = text?.Text;

			var parent = VisualTreeHelper.GetParent ((DependencyObject)sender);
			var button = (Button)VisualTreeHelper.GetChild (parent, 1);
			if (button.Command.CanExecute (input)) {
				button.Command.Execute (input);
				OnAddClick (sender, e);
			}
		}
	}
}
