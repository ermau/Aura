using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Aura
{
	internal class IsNullVisibilityConverter
		: IValueConverter
	{
		public bool Invert
		{
			get;
			set;
		}

		public object Convert (object value, Type targetType, object parameter, string language)
		{
			if (value == null || Invert)
				return Visibility.Collapsed;

			return Visibility.Visible;
		}

		public object ConvertBack (object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException ();
		}
	}
}
