using System;
using System.Collections.Generic;
using System.Text;

namespace Aura
{
	internal static class TypeExtensions
	{
		public static string GetSimpleTypeName (this Type type)
		{
			if (type == null)
				return null;

			return $"{type.FullName}, {type.Assembly.GetName ().Name}";
		}
	}
}
