using System;

namespace Aura.Data
{
	public class Timing
	{
		public TimeSpan MinStartDelay
		{
			get;
			set;
		}

		public TimeSpan MaxStartDelay
		{
			get;
			set;
		}

		public TimeSpan MinimumReoccurance
		{
			get;
			set;
		}

		public TimeSpan MaximumReoccurance
		{
			get;
			set;
		}

		public TimeSpan GetNextTime (Random rand, bool isFirst = false)
		{
			TimeSpan min = MinimumReoccurance;
			TimeSpan max = MaximumReoccurance;

			if (min.TotalMilliseconds < 0) {
				max += TimeSpan.FromMilliseconds (Math.Abs (min.TotalMilliseconds));
				min = TimeSpan.FromMilliseconds (0);
			}

			double p = rand.NextDouble ();
			long d = max.Ticks - min.Ticks;
			long ticks = (long)(p * d);
			if (MinimumReoccurance.TotalMilliseconds > 0 && !isFirst)
				ticks += MinimumReoccurance.Ticks;

			if (isFirst) {
				int delayTicks = (int)MinStartDelay.Ticks;
				if (MinStartDelay < MaxStartDelay)
					delayTicks = rand.Next ((int)MinStartDelay.Ticks, (int)MaxStartDelay.Ticks);

				ticks += delayTicks;
			}

			return TimeSpan.FromTicks (ticks);
		}
	}
}