using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aura
{
	public interface IPairedService
		: IService
	{
		/// <summary>
		/// Gets whether or not the <see cref="PairAsync(PairingOption, CancellationToken)"/> waits for an interaction with the paired service.
		/// </summary>
		/// <remarks>
		/// Generally speaking this would be used for pressing the button on a Hue bridge or similar such that the UI can display a interface
		/// to the user indicating it is waiting on them.
		/// </remarks>
		bool WaitsForUser { get; }

		/// <summary>
		/// Gets the name of the device being paired to, (e.g. 'bridge')
		/// </summary>
		string PairedDeviceName { get; }

		Task<IReadOnlyList<PairingOption>> GetPairingOptionsAsync (CancellationToken cancellation);
		Task<string> PairAsync (string id, CancellationToken cancellation);
		Task RestorePairAsync (string id, string pairing);
	}

	public record PairingOption
	{
		public string Id
		{
			get;
			init;
		}

		public string DisplayName
		{
			get;
			init;
		}
	}
}
