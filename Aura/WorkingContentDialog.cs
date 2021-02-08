using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Aura
{
	public class WorkingContentDialog
		: ContentDialog
	{
		public WorkingContentDialog()
		{
			Loaded += OnDialogLoaded;
		}

		private void OnDialogLoaded (object sender, RoutedEventArgs e)
		{
			var primaryButton = (Button)GetTemplateChild ("PrimaryButton");

			primaryButton.SetBinding (Button.CommandProperty, GetBindingExpression (PrimaryButtonCommandProperty).ParentBinding);
			ClearValue (PrimaryButtonCommandProperty);
			primaryButton.SetBinding (Button.CommandParameterProperty, GetBindingExpression (PrimaryButtonCommandParameterProperty).ParentBinding);
			ClearValue (PrimaryButtonCommandParameterProperty);
		}
	}
}
