using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record ContentAuthor
	{
		public string Name
		{
			get;
			init;
		}

		public string Url
		{
			get;
			init;
		}
	}
}
