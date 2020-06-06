using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Aura
{
	internal class BoolVisibilityConverter
		: IValueConverter
	{
		public bool Invert
		{
			get;
			set;
		}

		public object Convert (object value, Type targetType, object parameter, string language)
		{
			return (!Invert && ((value as bool?) ?? false)) ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack (object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException ();
		}
	}
}
