using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

using Q42.HueApi;

namespace Aura.Hue
{
	[Export (typeof(IService)), Shared]
	public class HueService
		: IDiscoverableService, IPairedService
	{
		public string DisplayName => "Philips Hue";
		public string PairedDeviceName => "bridge";

		public bool WaitsForUser => true;

		public async Task<bool> DiscoverAsync ()
		{
			return (await GetPairingOptionsAsync (CancellationToken.None)).Count > 0;
		}

		public async Task<IReadOnlyList<PairingOption>> GetPairingOptionsAsync (CancellationToken cancellation)
		{
			var bridges = await HueBridgeDiscovery.FastDiscoveryAsync (TimeSpan.FromSeconds (1));
			return bridges.Select (b => new PairingOption {
				DisplayName = $"{DisplayName} - {b.IpAddress}",
				Id = b.IpAddress.ToString ()
			}).ToArray ();
		}

		public async Task<string> PairAsync (string id, CancellationToken cancellation)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));

			string pairing;
			var client = new LocalHueClient (id);
			while (true) {
				cancellation.ThrowIfCancellationRequested ();

				try {
					pairing = await client.RegisterAsync ("Aura", Environment.MachineName);
					break;
				} catch (LinkButtonNotPressedException) {
					await Task.Delay (100);
				}
			}

			lock (this.clients) {
				this.clients.Add (client);
			}
			
			return pairing;
		}

		public Task RestorePairAsync (string id, string pairing)
		{
			if (string.IsNullOrWhiteSpace (pairing))
				throw new ArgumentException ($"'{nameof (pairing)}' cannot be null or whitespace", nameof (pairing));

			var client = new LocalHueClient (id);
			client.Initialize (pairing);
			lock (this.clients) {
				this.clients.Add (client);
			}

			return Task.CompletedTask;
		}

		private readonly List<HueClient> clients = new List<HueClient> ();
	}
}
