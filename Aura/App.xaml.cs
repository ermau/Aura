using System;
using System.Linq;
using System.Threading.Tasks;

using Aura.Data;
using Aura.Messages;
using Aura.Service.Client;
using Aura.Services;
using Aura.ViewModels;

using GalaSoft.MvvmLight.Messaging;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Aura
{
	sealed partial class App
		: Application
	{
		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			InitializeComponent();
			Suspending += OnSuspending;
		}

		internal static IAsyncServiceProvider Services
		{
			get;
			private set;
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			StartupServices();
			LaunchUi (e.PrelaunchActivated, e.PreviousExecutionState);
		}

		protected override async void OnActivated (IActivatedEventArgs args)
		{
			StartupServices ();

			if (args.Kind == ActivationKind.Protocol && args is ProtocolActivatedEventArgs activated) {
				/*
				 * aura://play/{id}[?ui=bool] - play `id` immediately
				 * aura://campaign/{id}[?][start=bool] - switch to campaign and show play page; if not found query web service and ask to join
				 */

				LaunchUi (prelaunched: false, activated.PreviousExecutionState);

				switch (activated.Uri.Host) {
					case "campaign":
						await ActivateCampaign (activated.Uri);
						break;
				}
			}
		}

		private void LaunchUi (bool prelaunched, ApplicationExecutionState previousState)
		{
			var tcs = new TaskCompletionSource<bool> ();
			GetStarted (tcs.Task);

			this.rootFrame = Window.Current.Content as Frame;
			if (rootFrame == null) {
				rootFrame = new Frame ();
				rootFrame.NavigationFailed += OnNavigationFailed;

				if (previousState == ApplicationExecutionState.Terminated) {
					//TODO: Load state from previously suspended application
				}

				Window.Current.Content = rootFrame;
			}

			if (!prelaunched) {
				if (rootFrame.Content == null) {
					// When the navigation stack isn't restored navigate to the first page,
					// configuring the new page by passing required information as a navigation
					// parameter
					rootFrame.Navigate (typeof (MainPage));
				}

				Window.Current.Activate ();
			}

			tcs.SetResult (true);
		}

		private Frame rootFrame;
		private AsyncServiceProvider serviceProvider;
		private ISyncService sync;

		private async Task ActivateCampaign (Uri uri)
		{
			CampaignManager campaigns = await Services.GetServiceAsync<CampaignManager> ();

			string campaignId = uri.Segments[1];
			CampaignElement campaign = campaigns.Elements.FirstOrDefault (c => c.Id == campaignId);
			if (campaign == null)
				return;

			campaigns.SelectedElement = campaign;
		}

		private async void StartupServices()
		{
			if (this.serviceProvider != null)
				return;

			this.serviceProvider = new AsyncServiceProvider (typeof (App).Assembly, typeof (Hue.HueService).Assembly, typeof (ILiveCampaignClient).Assembly);
			Services = this.serviceProvider;
			Messenger.Default.Register<ServiceDiscoveredMesage> (this, OnServiceDiscovered);
			
			this.serviceProvider.Expect<CampaignManager> ();
			this.serviceProvider.Expect<PlaySpaceManager> ();

			ISyncService sync = await this.serviceProvider.GetServiceAsync<ISyncService> ();
			ISettingsManager settings = await this.serviceProvider.GetServiceAsync<ISettingsManager> ();

			this.serviceProvider.Register (new CampaignManager (sync));
			this.serviceProvider.Register (new PlaySpaceManager (sync, settings));
		}

		private async void GetStarted(Task uiReady)
		{
			await GettingStarted.StartAsync (App.Services, uiReady);
		}

		private async void OnServiceDiscovered (ServiceDiscoveredMesage msg)
		{
			var playspaceManager = await this.serviceProvider.GetServiceAsync<PlaySpaceManager> ();
			bool isEnabled = await playspaceManager.GetIsServiceEnabledAsync (msg.Service);
			if (isEnabled)
				return;

			await this.rootFrame.Dispatcher.RunAsync (Windows.UI.Core.CoreDispatcherPriority.Low, () => {
				FlyoutService.PushFlyout ("ServiceAvailableFlyout", new EnableServiceRequestViewModel (msg.Service));
			});
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails
		/// </summary>
		/// <param name="sender">The Frame which failed navigation</param>
		/// <param name="e">Details about the navigation failure</param>
		void OnNavigationFailed (object sender, NavigationFailedEventArgs e)
		{
			throw new Exception ("Failed to load Page " + e.SourcePageType.FullName);
		}

		/// <summary>
		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		private void OnSuspending (object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			//TODO: Save application state and stop any background activity
			deferral.Complete();
		}
	}
}
