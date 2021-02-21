using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

using Q42.HueApi;
using Q42.HueApi.Models.Groups;

namespace Aura.Hue
{
	[Export (typeof (IService))]
	[Export (typeof (IEnvironmentService))]
	[Export (typeof (ILightingService))]
	[Shared]
	public class HueService
		: IDiscoverableService, IPairedService, ILightingService
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
				// TODO: This should be BridgeId, but it means RestorePairAsync needs to look up the bridges by ID
				// Could add a last-seen data to speed up lookup. Relying on IP for ID just means people are going to get unpaired
			}).ToArray ();
		}

		public Task StartAsync()
		{
			return Task.CompletedTask;
		}

		public Task StopAsync()
		{
			return Task.CompletedTask;
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

		public async Task<IReadOnlyList<LightGroup>> GetGroupsAsync (CancellationToken cancellation)
		{
			HueClient[] sclients;
			lock (this.clients) {
				sclients = this.clients.ToArray ();
			}

			List<LightGroup> lightGroups = new List<LightGroup> ();
			foreach (HueClient client in sclients) {
				cancellation.ThrowIfCancellationRequested ();

				var groups = await client.GetGroupsAsync ().ConfigureAwait (false);
				foreach (Group group in groups) {
					List<Light> lights = new List<Light> ();
					foreach (string lightId in group.Lights) {
						Position pos = null;
						if (group.Locations != null && group.Locations.TryGetValue (lightId, out LightLocation location)) {
							pos = new Position {
								X = (float)location[0],
								Y = (float)location[1],
								Z = (float)location[2]
							};
						}

						lights.Add (new Light {
							Id = lightId,
							Position = pos
						});
					}

					lightGroups.Add (new LightGroup {
						Id = group.Id,
						Name = group.Name,
						IsEntertainment = group.Type == GroupType.Entertainment,
						Lights = lights
					});
				}
			}

			return lightGroups;
		}

		private readonly List<HueClient> clients = new List<HueClient> ();
	}
}
