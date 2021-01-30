using System;
using System.Collections.Generic;
using System.Text;

using GalaSoft.MvvmLight;

namespace Aura.ViewModels
{
	internal class SettingsViewModel
		: ViewModelBase
	{
		public SettingsViewModel (IAsyncServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider ?? throw new ArgumentNullException (nameof (serviceProvider));
			Load ();
		}

		public bool SpoilerFree
		{
			get => this.settings.SpoilerFree;
			set => this.settings.SpoilerFree = value;
		}

		public bool VisualizePlayback
		{
			get => this.settings.VisualizePlayback;
			set => this.settings.VisualizePlayback = value;
		}

		public bool AutodetectPlaySpace
		{
			get => this.settings.AutodetectPlaySpace;
			set => this.settings.AutodetectPlaySpace = value;
		}

		private readonly IAsyncServiceProvider serviceProvider;
		private ISettingsManager settings;

		private async void Load()
		{
			this.settings = await this.serviceProvider.GetServiceAsync<ISettingsManager> ();
			this.settings.SettingsChanged += OnSettingsChanged;
		}

		private void OnSettingsChanged (object sender, EventArgs e)
		{
			RaisePropertyChanged (String.Empty);
		}
	}
}
