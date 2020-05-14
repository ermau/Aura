﻿using System;
using Moq;
using NUnit.Framework;

namespace Aura.Tests
{
	[TestFixture]
	public class AggregateProgressTests
	{
		[Test]
		public void SinglePercentNode()
		{
			var progress = new Mock<IProgress<double>> ();
			var aggregate = new AggregateProgress (progress.Object, holdForDiscovery: false);

			IProgress<double> node = aggregate.CreateProgressNode ();
			node.Report (5);

			progress.Verify (p => p.Report (5));
		}

		[Test]
		public void MultiplePercentNodes()
		{
			var progress = new Mock<IProgress<double>> ();
			var aggregate = new AggregateProgress (progress.Object, holdForDiscovery: false);

			IProgress<double> node = aggregate.CreateProgressNode ();
			IProgress<double> node2 = aggregate.CreateProgressNode ();

			node.Report (5);
			progress.Verify (p => p.Report (It.IsInRange (2.4, 2.51, Moq.Range.Inclusive)));

			node2.Report (5);
			progress.Verify (p => p.Report (It.IsInRange (4.9, 5.1, Moq.Range.Inclusive)));
		}
	}
}
