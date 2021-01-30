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
	}
}
