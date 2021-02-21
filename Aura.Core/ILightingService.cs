using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura
{
	public interface ILightingService
		: IEnvironmentService
	{
		Task<IReadOnlyList<LightGroup>> GetGroupsAsync (CancellationToken cancellationToken);
	}

	public record LightGroup
	{
		public string Id
		{
			get;
			init;
		}

		public string Name
		{
			get;
			init;
		}

		public bool IsEntertainment
		{
			get;
			init;
		}

		public IReadOnlyList<Light> Lights
		{
			get;
			init;
		} = Array.Empty<Light>();
	}

	public record Light
	{
		public string Id
		{
			get;
			init;
		}

		public Position Position
		{
			get;
			init;
		}
	}
}
