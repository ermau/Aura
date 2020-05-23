using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Messages
{
	internal class SingleSelectionChangedMessage
	{
		public SingleSelectionChangedMessage (Type type, NamedElement selectedElement)
		{
			Type = type;
			SelectedElement = selectedElement;
		}

		public Type Type
		{
			get;
		}

		public NamedElement SelectedElement
		{
			get;
		}
	}
}
