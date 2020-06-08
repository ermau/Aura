using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Q42.HueApi;

namespace Aura.Hue
{
	[Export (typeof(IService)), Shared]
	public class HueService
		: IDiscoverableService
	{
		public string DisplayName => "Philips Hue"; // TODO: Localize

		public async Task<bool> DiscoverAsync ()
		{
			var scan = new LocalNetworkScanBridgeLocator ();
			var bridges = await scan.LocateBridgesAsync (new CancellationTokenSource (5000).Token).ConfigureAwait (false);
			return bridges.Any ();
		}
	}
}
