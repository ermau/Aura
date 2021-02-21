using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Messages
{
	internal class ElementsChangedMessage
	{
		public ElementsChangedMessage (Type type, string id)
		{
			Type = type ?? throw new ArgumentNullException (nameof (type));
			Id = id ?? throw new ArgumentNullException (nameof (id));
		}

		public Type Type { get; }
		public string Id { get; }
	}
}
