using System;

using Windows.Media.Core;
using Windows.UI.Xaml.Data;

namespace Aura
{
	internal class MediaSourceConverter
		: IValueConverter
	{
		public object Convert (object value, Type targetType, object parameter, string language)
		{
			Uri uri = value as Uri;
			if (uri != null)
				return MediaSource.CreateFromUri (uri);

			return null;
		}

		public object ConvertBack (object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException ();
		}
	}
}
