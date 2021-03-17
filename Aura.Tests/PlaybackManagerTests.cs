using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aura.Services;

using Moq;

using NUnit.Framework;

namespace Aura.Tests
{
	[TestFixture]
	public class PlaybackManagerTests
	{
		[SetUp]
		public void Setup()
		{
			this.services = new AsyncServiceProvider ();
		}

		[Test]
		public void PlayFiresChangeEvent()
		{
			var manager = new PlaybackManager (services);

			var environment = new PlaybackEnvironment (new[] {
				new PlaybackEnvironmentElement (new Data.EncounterStateElement {
					ElementId = "elementId1",
					StartsWithState = true
				},
				new Data.EnvironmentElement {
					Id = "elementId1",
					Name = "Element"
				})
			});

			bool envChanged = false;
			manager.EnvironmentChanged += (o, e) => {
				Assert.That (e.PreviousEnvironment, Is.Null);
				Assert.That (e.NewEnvironment, Is.SameAs (environment));
				envChanged = true;
			};

			manager.PlayEnvironment (environment);
			Assert.That (envChanged, Is.True);
		}

		[Test]
		public void StopFiresChangeEvent()
		{
			var manager = new PlaybackManager (services);

			var environment = new PlaybackEnvironment (new[] {
				new PlaybackEnvironmentElement (new Data.EncounterStateElement {
					ElementId = "elementId1",
					StartsWithState = true
				},
				new Data.EnvironmentElement {
					Id = "elementId1",
					Name = "Element"
				})
			});

			manager.PlayEnvironment (environment);

			bool envChanged = false;
			manager.EnvironmentChanged += (o, e) => {
				Assert.That (e.PreviousEnvironment, Is.SameAs (environment));
				Assert.That (e.NewEnvironment, Is.Null);
				envChanged = true;
			};

			manager.Stop ();
			Assert.That (envChanged, Is.True);
		}

		private AsyncServiceProvider services;
	}
}
