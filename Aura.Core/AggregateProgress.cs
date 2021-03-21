using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Aura
{
	public class AggregateProgress
	{
		public AggregateProgress (IProgress<double> progress, bool holdForDiscovery = true)
		{
			if (progress == null)
				throw new ArgumentNullException (nameof (progress));

			this.progress = progress;
			this.holdForDiscovery = holdForDiscovery;

			this.context = SynchronizationContext.Current;
		}

		public void FinishDiscovery (int nodes = 0)
		{
			this.holdForDiscovery = false;
			if (nodes > 0) {
				this.preNodes = new Stack<ProgressNodePercent> ();
				for (int i = 0; i < nodes; i++) {
					Interlocked.Add (ref this.total, ProgressNodePercent.PercentTotal);
					this.preNodes.Push (new ProgressNodePercent (this));
				}
			}

			ReportCurrent ();
		}

		public IProgress<double> PopNode()
		{
			if (this.preNodes == null)
				throw new InvalidOperationException ("Finish discovery before popping pre-allocated nodes");
			if (this.preNodes.Count == 0)
				throw new InvalidOperationException ("Pre-allocated nodes have all been used");

			return this.preNodes.Pop ();
		}

		public IProgress<double> CreateProgressNode ()
		{
			long t = Interlocked.Add (ref this.total, ProgressNodePercent.PercentTotal);
			var node = new ProgressNodePercent (this);

			ReportCurrent ();
			return node;
		}

		public IProgress<int> CreateProgressNode (int maxValue)
		{
			long t = Interlocked.Add (ref this.total, maxValue);
			var node = new ProgressNodeInt (this);

			ReportCurrent ();
			return node;
		}

		public IProgress<long> CreateProgressNode (long maxValue)
		{
			long t = Interlocked.Add (ref this.total, maxValue);
			var node = new ProgressNode (this);

			ReportCurrent ();
			return node;
		}

		private readonly SynchronizationContext context;
		private readonly IProgress<double> progress;
		private Stack<ProgressNodePercent> preNodes;
		private bool holdForDiscovery;

		private long total;
		private long current;

		private void AddProgress (long progress)
		{
			if (progress <= 0)
				return;

			long t = Interlocked.Read (ref this.total);
			long c = Interlocked.Add (ref this.current, progress);

			if (!this.holdForDiscovery)
				Report ((double)c / t);
		}

		private void Report (double value)
		{
			if (this.context != null)
				this.context.Post (s => this.progress.Report ((double)s), value);
			else
				this.progress.Report (value);
		}

		private void ReportCurrent ()
		{
			if (this.holdForDiscovery)
				return;

			long c = Interlocked.Read (ref this.current);
			if (c == 0)
				return;

			long t = Interlocked.Read (ref this.total);
			Report ((double)c / t);
		}

		private class ProgressNodePercent
			: IProgress<double>
		{
			public const int PercentTotal = 10000;

			public ProgressNodePercent (AggregateProgress parent)
			{
				this.parent = parent;
			}

			public void Report (double value)
			{
				double p = Interlocked.Exchange (ref this.last, value);
				long d = (long)((value - p) * PercentTotal);

				this.parent.AddProgress (d);
			}

			private double last;
			private readonly AggregateProgress parent;
		}

		private class ProgressNodeInt
			: IProgress<int>
		{
			public ProgressNodeInt (AggregateProgress parent)
			{
				this.parent = parent;
			}

			public void Report (int value)
			{
				int p = Interlocked.Exchange (ref this.last, value);
				int d = value - p;

				this.parent.AddProgress (d);
			}

			private int last;
			private readonly AggregateProgress parent;
		}

		private class ProgressNode
			: IProgress<long>
		{
			public ProgressNode (AggregateProgress parent)
			{
				this.parent = parent;
			}

			public void Report (long value)
			{
				long p = Interlocked.Exchange (ref this.last, value);
				long d = value - p;

				this.parent.AddProgress (d);
			}

			private long last;
			private readonly AggregateProgress parent;
		}
	}
}
