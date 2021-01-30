using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public record ReverbElement
		: Element
	{
		public double WetDry
		{
			get;
			init;
		}

		public double Density
		{
			get;
			init;
		}

		public double DecayTime
		{
			get;
			init;
		}

		public double RoomFrequency
		{
			get;
			init;
		}

		public double RoomHighFrequency
		{
			get;
			init;
		}
	}
}
