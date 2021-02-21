using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;

using Aura.Data;
using Aura.Messages;
using System.Threading;

namespace Aura
{
	internal class PlaySpaceManager
		: SingleSelectionManager<PlaySpaceElement>
	{
		public PlaySpaceManager (ISyncService syncService, ISettingsManager settings)
			: base (syncService)
		{
			Messenger.Default.Register<EnableServiceMessage> (this, OnEnableService);
			this.settings = settings ?? throw new ArgumentNullException (nameof (settings));
		}

		/// <summary>
		/// Gets the enabled services for the current play space out of the <paramref name="availableServices"/>.
		/// </summary>
		public IEnumerable<IEnvironmentService> GetServices (IEnumerable<IEnvironmentService> availableServices)
		{
			if (availableServices is null)
				throw new ArgumentNullException (nameof (availableServices));

			PlaySpaceElement space = SelectedElement;
			return (space != null)
				? availableServices.Where (s => space.Services.Contains (s.GetType ().GetSimpleTypeName ()))
				: Enumerable.Empty<IEnvironmentService> ();
		}

		public async Task EnableServiceAsync (IService service)
		{
			if (service is null)
				throw new ArgumentNullException (nameof (service));

			PlaySpaceElement space = SelectedElement;
			string serviceName = service.GetType ().GetSimpleTypeName ();

			if (service is IPairedService pairedService) {
				var msg = new PairServiceMessage (pairedService);
				Messenger.Default.Send (msg);
				if (msg.Result == null) {
					throw new OperationCanceledException ();
				}

				var servicePairings = space.Pairings?.ToDictionary (kvp => kvp.Key, kvp => (IReadOnlyList<PairedDevice>)new List<PairedDevice> (kvp.Value))
					?? new Dictionary<string, IReadOnlyList<PairedDevice>>();
				if (!servicePairings.TryGetValue (serviceName, out var pairings))
					servicePairings[serviceName] = pairings = new List<PairedDevice> ();
				
				var pairing = await msg.Result;
				((List<PairedDevice>)pairings).Add (new PairedDevice {
					Id = pairing.Item1.Id,
					Pair = pairing.Item2
				});

				space = space with { Pairings = servicePairings };
			}

			// Paired services may ask user to enable if their only paired devices aren't present, but we don't need to re-add the service
			if (!space.Services.Contains (serviceName)) {
				var services = new List<string> (space.Services.Count + 1);
				services.AddRange (space.Services);
				services.Add (serviceName);

				space = space with { Services = services };
			}

			await SyncService.SaveElementAsync (space);
			await Loading;
		}

		public async Task<bool> TryRestoreServiceAsync (IService service)
		{
			if (service is IPairedService paired) {
				await Loading;
				string serviceName = service.GetType ().GetSimpleTypeName ();
				var e = SelectedElement;
				if (e == null || !e.Services.Contains (serviceName))
					return false;

				if (e.Pairings != null && e.Pairings.TryGetValue (serviceName, out var pairings)) {
					bool restoredPair = false;
					foreach (var pair in pairings) {
						await paired.RestorePairAsync (pair.Id, pair.Pair);
						restoredPair = true;
					}

					return restoredPair;
				} else
					return true;
			} else {
				return await GetIsServiceEnabledAsync (service).ConfigureAwait (false);
			}
		}

		public async Task<bool> GetIsServiceEnabledAsync (IService service)
		{
			if (service is null)
				throw new ArgumentNullException (nameof (service));

			await Loading;
			string serviceName = service.GetType ().GetSimpleTypeName ();
			var e = SelectedElement;
			if (e == null || !e.Services.Contains (serviceName))
				return false;

			// If it's a paired service, we need to ensure the paired component is still available
			if (service is IPairedService paired) {
				if (e.Pairings == null || !e.Pairings.TryGetValue (serviceName, out var pairings))
					return false;

				var source = new CancellationTokenSource (5000);
				try {
					var options = await paired.GetPairingOptionsAsync (source.Token);
					var availableDevices = new HashSet<string> (options.Select (po => po.Id));
					return pairings.Any (pd => availableDevices.Contains (pd.Id));
				} catch (Exception) {
					// TODO log in case not cancel
					return false;
				}
			}

			return true;
		}

		private ISettingsManager settings;

		private async void OnEnableService (EnableServiceMessage msg)
		{
			PairServiceResultMessage resultMsg = null;
			try {
				await EnableServiceAsync (msg.Service);
					
				if (msg.Service is IPairedService paired) {
					resultMsg = new PairServiceResultMessage (paired);
				}
			} catch (OperationCanceledException) {
			} catch (Exception ex) {
				if (msg.Service is IPairedService paired) {
					resultMsg = new PairServiceResultMessage (paired, ex);
				}
			}

			if (resultMsg != null)
				Messenger.Default.Send (resultMsg);
		}
	}
}