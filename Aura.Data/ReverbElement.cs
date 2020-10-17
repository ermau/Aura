using System;
using System.Collections.Generic;
using System.Text;

namespace Aura.Data
{
	public class ReverbElement
		: Element
	{
		public double WetDry
		{
			get;
			set;
		}

		public double Density
		{
			get;
			set;
		}

		public double DecayTime
		{
			get;
			set;
		}

		public double RoomFrequency
		{
			get;
			set;
		}

		public double RoomHighFrequency
		{
			get;
			set;
		}
	}
}
