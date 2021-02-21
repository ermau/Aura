using System;

namespace Aura.Data
{
	public record Timing
	{
		public TimeSpan MinStartDelay
		{
			get;
			init;
		}

		public TimeSpan MaxStartDelay
		{
			get;
			init;
		}

		public TimeSpan MinimumReoccurance
		{
			get;
			init;
		}

		public TimeSpan MaximumReoccurance
		{
			get;
			init;
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