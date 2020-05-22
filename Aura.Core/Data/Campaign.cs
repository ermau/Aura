using System;
using System.Collections.Generic;
using System.Text;

namespace Aura
{
	internal class Campaign
		: NamedElement
	{
		public Guid Secret
		{
			get;
			set;
		}

		public bool IsRemote
		{
			get;
			set;
		}
	}
}
