using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record SingleSelectionElement
		: Element
	{
		public string Type;
		public string SelectionId;
	}
}
