using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aura.Data;
using Aura.Services;
using Aura.ViewModels;

using NUnit.Framework;

namespace Aura.Tests
{
	[TestFixture]
	public class RunEncounterStateViewModelTests
	{
		[SetUp]
		public void Setup()
		{
			this.serviceProvider = new AsyncServiceProvider ();
			this.serviceProvider.Expect<ISyncService> ();
			this.serviceProvider.Expect<PlaySpaceManager> ();
			this.serviceProvider.MockService<ILocalStorageService> ();

			this.serviceProvider.Register<ISyncService> (this.sync = new MockSyncService ());
			this.serviceProvider.Register (this.settings = new SettingsManager (this.sync));
			this.serviceProvider.Register (new DownloadManager (this.serviceProvider));
			this.serviceProvider.Register (new PlaySpaceManager (this.sync, this.settings));
			this.serviceProvider.Register (new PlaybackManager (this.serviceProvider));
		}

		[Test]
		public void Play()
		{
			this.sync.SaveElementAsync (new EnvironmentElement {
				Id = "ElementId1"
			}).Wait ();

			var vm = new RunEncounterStateViewModel (this.serviceProvider, new EncounterState {
				Name = "State",
				EnvironmentElements = new [] {
					new EncounterStateElement {
						ElementId = "ElementId1",
						StartsWithState = true
					}
				}
			});

			Assume.That (vm.IsPlaying, Is.False);

			Assert.That (vm.PlayCommand.CanExecute (null), Is.True);
			Assert.That (vm.StopCommand.CanExecute (null), Is.False);

			bool playingChanged = false;
			vm.PropertyChanged += (o, e) => {
				if (e.PropertyName == nameof (vm.IsPlaying))
					playingChanged = true;
			};

			vm.PlayCommand.Execute (null);

			Assert.That (playingChanged, Is.True);
		}

		private AsyncServiceProvider serviceProvider;
		private MockSyncService sync;
		private SettingsManager settings;
	}
}
