using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura
{
	[Export (typeof(SettingsManager)), Shared]
	internal class SettingsManager
	{
		[ImportingConstructor]
		public SettingsManager (ISyncService syncService)
		{
			this.syncService = syncService ?? throw new ArgumentNullException (nameof (syncService));
			Reload ();
		}

		public event EventHandler SettingsChanged
		{
			add { this.changedEvent.Subscribe (value); }
			remove { this.changedEvent.Unsubscribe (value); }
		}

		public Task Loading => this.loadTask;

		public bool SpoilerFree
		{
			get => this.settings.SpoilerFree;
			set { Save (this.settings with { SpoilerFree = value }); }
		}

		public bool VisualizePlayback
		{
			get => this.settings.VisualizePlayback;
			set { Save (this.settings with { VisualizePlayback = value }); }
		}

		public bool AutodetectPlaySpace
		{
			get => this.settings.AutodetectPlaySpace;
			set { Save (this.settings with { AutodetectPlaySpace = value }); }
		}

		public bool DownloadInBackground
		{
			get => this.settings.DownloadInBackground;
			set { Save (this.settings with { DownloadInBackground = value }); }
		}

		private readonly ISyncService syncService;
		private readonly SemaphoreSlim sync = new (1);
		private readonly AsyncEventManager<EventHandler> changedEvent = new ();

		private Task loadTask;
		private SettingsElement settings;

		private void Reload()
		{
			this.loadTask = LoadAsync ();
		}

		private async Task LoadAsync ()
		{
			await this.sync.WaitAsync ().ConfigureAwait(false);
			try {
				this.settings = (await this.syncService.GetElementsAsync<SettingsElement> ()).FirstOrDefault();
				if (this.settings == null) {
					this.settings = new SettingsElement ();
				}
			} finally {
				this.sync.Release ();
			}
		}

		private async void Save (SettingsElement newSettings)
		{
			await SaveAsync (newSettings);
		}

		private async Task SaveAsync (SettingsElement newSettings)
		{
			await this.loadTask;
			await this.sync.WaitAsync ().ConfigureAwait (false);
			try {
				this.settings = await this.syncService.SaveElementAsync (newSettings);
			} finally {
				this.sync.Release ();
			}

			this.changedEvent.Invoke (this, EventArgs.Empty);
		}
	}
}
