using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

using NUnit.Framework;

namespace Aura.Tests
{
	[TestFixture]
	public class PlaybackEnvironmentTests
	{
		/*
		[Test]
		public void ClearTransitionsWhileInactive()
		{
			// We need to ensure we don't pick up an old transition if we
			// transition in and out.

			var playbackElement = new PlaybackEnvironmentElement (
				new EncounterStateElement {
					ElementId = "ElementId1",
					StartsWithState = true
				},
				new EnvironmentElement {
					Id = "ElementId1"
				});

			var env = new PlaybackEnvironment (new[] { playbackElement });

			env.FadeInAsync (TimeSpan.FromMilliseconds (10));
			Thread.Sleep (15);

			Assume.That (playbackElement.IsActive, Is.True);
			Assume.That (playbackElement.Intensity, Is.EqualTo (1));
		}*/
	}
}
