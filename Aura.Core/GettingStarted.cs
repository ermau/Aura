using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Linq;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;

using Aura.Data;
using Aura.Messages;

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
				IDiscoverableService[] dservices = (await services.GetServicesAsync<IService> ()).OfType<IDiscoverableService> ().ToArray ();

				var results = new Dictionary<Task<bool>, IDiscoverableService> (dservices.Length);
				for (int i = 0; i < dservices.Length; i++) {
					results.Add (dservices[i].DiscoverAsync (), dservices[i]);
				}

				while (results.Count > 0) {
					Task<bool> completed = await Task.WhenAny (results.Keys);
					IDiscoverableService service = results[completed];
					results.Remove (completed);

					Messenger.Default.Send (new ServiceDiscoveredMesage (service));
				}
			});
		}
	}
}