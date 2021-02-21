using System;
using System.Collections.Generic;
using System.Linq;

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
		} = Array.Empty<string> ();

		public IReadOnlyDictionary<string, IReadOnlyList<PairedDevice>> Pairings
		{
			get;
			init;
		} = new Dictionary<string, IReadOnlyList<PairedDevice>> ();

		public IReadOnlyList<LightingConfiguration> LightingConfigurations
		{
			get;
			init;
		} = Array.Empty<LightingConfiguration> ();
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
