using System;
using System.Collections.Generic;
using System.Text;

namespace Aura
{
	public interface IService
	{
		/// <summary>
		/// Gets a localized display name for the service.
		/// </summary>
		/// <remarks>This is used in settings and prompts to enable the service.</remarks>
		string DisplayName { get; }
	}
}
