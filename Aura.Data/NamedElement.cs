using System;
using System.Collections.Generic;

namespace Aura.Data
{
	public record NamedElement
		: Element
	{
		public string Name
		{
			get;
			init;
		}

		public IReadOnlyCollection<string> Tags
		{
			get;
			init;
		} = Array.Empty<string> ();
	}
}