using System;
using System.Collections.Generic;
using System.Text;

namespace Aura
{
	internal abstract class Element
	{
		public string Id
		{
			get;
			set;
		}
	}

	internal class NamedElement
		: Element
	{
		public string Name
		{
			get;
			set;
		}
	}
}