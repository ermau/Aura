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
			bool v = (value as bool?) ?? false;
			if (v && !Invert || !v && Invert)
				return Visibility.Visible;

			return Visibility.Collapsed;
		}

		public object ConvertBack (object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException ();
		}
	}
}
