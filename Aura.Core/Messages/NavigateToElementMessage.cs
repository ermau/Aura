using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Messages
{
	internal class NavigateToElementMessage
	{
		public NavigateToElementMessage (string id, Type type)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));
			if (type is null)
				throw new ArgumentNullException (nameof (type));

			Id = id;
			Type = type;
		}

		public string Id
		{
			get;
		}

		public Type Type
		{
			get;
		}
	}
}