using System;
using System.Collections.Generic;
using System.Text;

namespace Aura
{
	public class Campaign
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
