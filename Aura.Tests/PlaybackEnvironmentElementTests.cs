using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aura.Data;

using Moq;

using NUnit.Framework;

namespace Aura.Tests
{
	[TestFixture]
	public class PlaybackEnvironmentElementTests
	{
		[Test]
		public void PrepareFirstElement()
		{
			const string elementId = "eID";
			const string sourceId = "ID1";
			var element = new EnvironmentElement {
				Id = elementId,
				Positioning = new Positioning {
					FixedPosition = new Position ()
				},

				Timing = new Timing {
					MinStartDelay = TimeSpan.FromMinutes (1),
					MaxStartDelay = TimeSpan.FromMinutes (1)
				},

				Audio = new AudioComponent {
					Playlist = new ElementPlaylist {
						Order = SourceOrder.InOrder,
						Repeat = SourceRepeatMode.None,
						Descriptors = new [] {
							sourceId
						}
					}
				}
			};

			var state = new EncounterStateElement {
				ElementId = elementId,
				StartsWithState = true
			};

			var audio = new Mock<IAudioService> ();
			var storage = new Mock<ILocalStorageService> ();

			var environmentElement = new PlaybackEnvironmentElement (state, element);
			environmentElement.Tick (new ActiveServices {
				Audio = audio.Object,
				Storage = storage.Object
			});

			audio.Verify (a => a.PrepareEffectAsync (element, sourceId, It.IsAny<PlaybackOptions>()));
		}

		[Test]
		public void PlayFirstElement ()
		{
			const string elementId = "eID";
			const string sourceId = "ID1";
			var element = new EnvironmentElement {
				Id = elementId,
				Positioning = new Positioning {
					FixedPosition = new Position ()
				},

				Timing = new Timing {
					MinStartDelay = TimeSpan.FromMinutes (0),
					MaxStartDelay = TimeSpan.FromMinutes (0)
				},

				Audio = new AudioComponent {
					Playlist = new ElementPlaylist {
						Order = SourceOrder.InOrder,
						Repeat = SourceRepeatMode.None,
						Descriptors = new[] {
							sourceId
						}
					}
				}
			};

			var state = new EncounterStateElement {
				ElementId = elementId,
				StartsWithState = true
			};

			var audio = new Mock<IAudioService> ();
			var storage = new Mock<ILocalStorageService> ();

			var effect = new TestEffect (TimeSpan.FromSeconds (30));
			audio.Setup (a => a.PrepareEffectAsync (It.IsAny<EnvironmentElement> (), It.IsAny<string> (), It.IsAny<PlaybackOptions> ()))
				.ReturnsAsync (effect);

			var environmentElement = new PlaybackEnvironmentElement (state, element);
			environmentElement.Tick (new ActiveServices {
				Audio = audio.Object,
				Storage = storage.Object
			});

			audio.Verify (a => a.PrepareEffectAsync (element, sourceId, It.IsAny<PlaybackOptions> ()));
			audio.Verify (a => a.PlayEffect (effect));
		}

		private class TestEffect
			: IPreparedEffect
		{
			public TestEffect (TimeSpan duration)
			{
				Duration = duration;
			}

			public TimeSpan Duration
			{
				get;
			}

			public void Dispose ()
			{
			}
		}
	}
}
