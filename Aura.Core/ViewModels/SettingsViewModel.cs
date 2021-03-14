using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

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

		public bool DownloadInBackground
		{
			get => this.settings.DownloadInBackground;
			set => this.settings.DownloadInBackground = value;
		}

		public IReadOnlyList<AuthedServiceViewModel> AuthedServices
		{
			get => this.authedServices;
			private set
			{
				if (this.authedServices == value)
					return;

				this.authedServices = value;
				RaisePropertyChanged ();
			}
		}

		private readonly IAsyncServiceProvider serviceProvider;
		private SettingsManager settings;
		private IReadOnlyList<AuthedServiceViewModel> authedServices;

		private async void Load()
		{
			this.settings = await this.serviceProvider.GetServiceAsync<SettingsManager> ();
			this.settings.SettingsChanged += OnSettingsChanged;

			AuthedServices = (await this.serviceProvider.GetServicesAsync<IAuthenticatedService> ())
				.Select (s => new AuthedServiceViewModel (this.serviceProvider, s))
				.ToArray ();
		}

		private void OnSettingsChanged (object sender, EventArgs e)
		{
			RaisePropertyChanged (String.Empty);
		}
	}

	internal class AuthedServiceViewModel
		: BusyViewModel
	{
		public AuthedServiceViewModel (IAsyncServiceProvider services, IAuthenticatedService service)
		{
			this.services = services ?? throw new ArgumentNullException (nameof (services));
			Service = service ?? throw new ArgumentNullException (nameof (service));
			LoginCommand = new RelayCommand (OnLogin);
			LogoutCommand = new RelayCommand (OnLogout);
			Load ();
		}

		public string Username
		{
			get => this.username;
			set
			{
				if (this.username == value)
					return;

				this.username = value;
				RaisePropertyChanged ();
			}
		}

		public bool IsLoggedIn
		{
			get => this.isLoggedIn;
			set
			{
				if (this.isLoggedIn == value)
					return;

				this.isLoggedIn = value;
				RaisePropertyChanged ();
			}
		}

		public ICommand LoginCommand
		{
			get;
		}

		public ICommand LogoutCommand
		{
			get;
		}

		public IAuthenticatedService Service { get; }

		private IAuthenticationService auth;
		private readonly IAsyncServiceProvider services;
		private bool isLoggedIn;
		private string username;

		private async void Load ()
		{
			AddWork ();

			this.auth = await this.services.GetServiceAsync<IAuthenticationService> ();

			if (!await this.auth.TryAuthenticateAsync (Service, CancellationToken.None)) {
				FinishWork ();
				return;
			}

			try {
				Username = await Service.GetUsernameAsync (CancellationToken.None);
			} catch {
			}

			IsLoggedIn = (Username != null);
			FinishWork ();

			await ((IContentProviderService)Service).SearchAsync (new ContentSearchOptions { Query = "lightning" }, CancellationToken.None);
		}

		private async void OnLogin ()
		{
			AddWork ();
			try {
				IsLoggedIn = await this.auth.AuthenticateAsync (Service);
				Username = await Service.GetUsernameAsync (CancellationToken.None);
			} catch {
				IsLoggedIn = false;
				Username = null;
			}
			FinishWork ();
		}

		private async void OnLogout ()
		{
			await this.auth.LogoutAsync (Service);
			IsLoggedIn = false;
			Username = null;
		}
	}
}
