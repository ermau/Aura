using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Linq;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;

using Aura.Data;
using Aura.Messages;
using System.Threading;

namespace Aura
{
	internal static class GettingStarted
	{
		public static async Task StartAsync (IAsyncServiceProvider services, Task uiReady)
		{
			if (services is null)
				throw new ArgumentNullException (nameof (services));

			Task playspacesSetup = RunPlayspacesSetupAsync (services);
			await uiReady;
			await RunCampaignSetupAsync (services);
			await playspacesSetup;
			await RunServiceDiscoveryAsync (services);
		}

		private static async Task RunCampaignSetupAsync (IAsyncServiceProvider services)
		{
			var campaigns = await services.GetServiceAsync<CampaignManager> ();
			await campaigns.Loading;

			if (campaigns.Elements.Count == 0) {
				Messenger.Default.Send (new RequestCampaignPromptMessage ());
			}
		}

		private static async Task RunPlayspacesSetupAsync (IAsyncServiceProvider services)
		{
			var playspaces = await services.GetServiceAsync<PlaySpaceManager> ().ConfigureAwait (false);
			await playspaces.Loading.ConfigureAwait (false);

			if (playspaces.Elements.Count == 0) {
				IAudioService audioService = await services.GetServiceAsync<IAudioService> ();

				var home = new PlaySpaceElement {
					Name = "Home",
					Services = new [] { audioService.GetType().GetSimpleTypeName() }
				};
				ISyncService sync = await services.GetServiceAsync<ISyncService> ().ConfigureAwait (false);
				await sync.SaveElementAsync (home).ConfigureAwait (false);
				await playspaces.Loading.ConfigureAwait (false);
			}
		}

		private static Task RunServiceDiscoveryAsync (IAsyncServiceProvider services)
		{
			return Task.Run (async () => {

				IAuthenticationService auth = null;

				List<Task> tasks = new List<Task> ();

				IDiscoverableService[] discoverableServices = await services.GetServicesAsync<IDiscoverableService> ();
				foreach (IDiscoverableService discoverable in discoverableServices) {
					tasks.Add (DiscoverService (discoverable));
				}

				IAuthenticatedService[] authenticatedServices = await services.GetServicesAsync<IAuthenticatedService> ();
				foreach (IAuthenticatedService authed in authenticatedServices) {
					if (auth == null)
						auth = await services.GetServiceAsync<IAuthenticationService> ();

					tasks.Add (TryAuth (auth, authed));
				}

				await Task.WhenAll (tasks);
			});
		}

		private static async Task TryAuth (IAuthenticationService auth, IAuthenticatedService authed)
		{
			try {
				await auth.TryAuthenticateAsync (authed, new CancellationTokenSource (5000).Token);
			} catch (OperationCanceledException) {
			}
		}

		private static async Task DiscoverService (IDiscoverableService service)
		{
			bool discovered = await service.DiscoverAsync ();
			if (discovered) {
				Messenger.Default.Send (new ServiceDiscoveredMesage (service));
			}
		}
	}
}