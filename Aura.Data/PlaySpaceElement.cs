using System.Collections.Generic;

namespace Aura.Data
{
	public record PlaySpaceElement
		: NamedElement
	{
		/// <summary>
		/// Gets or sets a list of types for enabled services for this playspace.
		/// </summary>
		public IReadOnlyList<string> Services
		{
			get;
			init;
		}

		public IReadOnlyDictionary<string, IReadOnlyList<PairedDevice>> Pairings
		{
			get;
			init;
		}
	}

	public record PairedDevice
	{
		public string Id
		{
			get;
			init;
		}

		public string Pair
		{
			get;
			init;
		}
	}
}
