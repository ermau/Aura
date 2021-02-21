using System;
using System.Collections.Generic;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

using Aura.Data;

namespace Aura
{
	internal class IconConverter
		: IValueConverter
	{
		public object Convert (object value, Type targetType, object parameter, string language)
		{
			if (value != null && IconMap.TryGetValue (value.GetType(), out (Symbol, string) either)) {
				if (either.Item2 == null)
					return new SymbolIcon (either.Item1);
				else
					return new FontIcon { Glyph = either.Item2 };
			}

			return null;
		}

		public object ConvertBack (object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException ();
		}

		private static readonly Dictionary<Type, (Symbol, string)> IconMap = new Dictionary<Type, (Symbol, string)> {
			{ typeof(CampaignElement), (Symbol.World, null) },
			{ typeof(PlaySpaceElement), (Symbol.Home, null) },
			{ typeof(EnvironmentElement), (Symbol.Preview, null) },
			{ typeof(FileSample), (Symbol.Audio, null) },
			{ typeof(EncounterElement), (Symbol.People, null) }
		};
	}
}
