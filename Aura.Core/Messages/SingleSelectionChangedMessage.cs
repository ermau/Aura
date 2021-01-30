using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Aura.Data;

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

	internal class SingleSelectionPreviewChangeMessage
		: SingleSelectionChangedMessage
	{
		public SingleSelectionPreviewChangeMessage (Type type, NamedElement selectedElement)
			: base (type, selectedElement)
		{
		}

		public Task<bool> Canceled
		{
			get;
			set;
		}
	}
}
