using System;
using System.Composition.Hosting;
using System.Threading.Tasks;
using Aura.Service.Client;
using Aura.Services;
using Aura.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Xamarin.Essentials.Implementation;
using Xamarin.Essentials.Interfaces;

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

		public static Task<CompositionHost> CompositionHost
		{
			get;
			private set;
		}

		public static async Task<T> GetServiceAsync<T>()
		{
			if (typeof (T).Assembly.FullName.StartsWith ("Xamarin")) {
				return SimpleIoc.Default.GetInstance<T> ();
			} else {
				var host = await CompositionHost.ConfigureAwait (false);
				return host.GetExport<T> ();
			}
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			GettingStarted.ServiceDiscovered += OnServiceDiscovered;

			var uiReady = new TaskCompletionSource<bool> ();
			SetupCompositionAsync ().ContinueWith (async t => {
				await GettingStarted.StartAsync (t.Result, uiReady.Task);
			}, TaskScheduler.FromCurrentSynchronizationContext());

			this.rootFrame = Window.Current.Content as Frame;
			if (rootFrame == null) {
				rootFrame = new Frame();
				rootFrame.NavigationFailed += OnNavigationFailed;

				if (e.PreviousExecutionState == ApplicationExecutionState.Terminated) {
					//TODO: Load state from previously suspended application
				}

				Window.Current.Content = rootFrame;
			}

			if (e.PrelaunchActivated == false) {
				if (rootFrame.Content == null) {
					// When the navigation stack isn't restored navigate to the first page,
					// configuring the new page by passing required information as a navigation
					// parameter
					rootFrame.Navigate(typeof(MainPage), e.Arguments);
				}

				Window.Current.Activate();
				uiReady.SetResult (true);
			}
		}

		private Frame rootFrame;

		private async void OnServiceDiscovered (object sender, ServiceDiscoveredEventArgs e)
		{
			await this.rootFrame.Dispatcher.RunAsync (Windows.UI.Core.CoreDispatcherPriority.Low, () => {
				FlyoutService.PushFlyout ("ServiceAvailableFlyout", new EnableServiceRequestViewModel (e.Service));
			});
		}

		private Task<CompositionHost> SetupCompositionAsync()
		{
			SimpleIoc.Default.Register<IPreferences, PreferencesImplementation> ();

			return CompositionHost = Task.Run (() => {
				ContainerConfiguration configuration = new ContainerConfiguration ();
				return configuration.WithAssemblies (new[] {
					typeof (App).Assembly,
					typeof (Aura.Hue.HueService).Assembly,
					typeof (ILiveCampaignClient).Assembly
				}).CreateContainer();
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
