using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Messages
{
	internal class ElementsChangedMessage
	{
		public ElementsChangedMessage (Type type)
		{
			Type = type ?? throw new ArgumentNullException (nameof (type));
		}

		public Type Type { get; }
	}
}
