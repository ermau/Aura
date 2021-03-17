using System;
using System.Collections.Generic;
using System.Text;

using Aura.Data;

namespace Aura
{
	internal record ActiveServices
	{
		public IAudioService Audio
		{
			get;
			init;
		}

		public ILightingService Lighting
		{
			get;
			init;
		}

		public ILocalStorageService Storage
		{
			get;
			init;
		}

		public IEnvironmentService GetService (EnvironmentComponent component)
		{
			if (component is AudioComponent)
				return Audio;
			if (component is LightingComponent)
				return Lighting;

			return null;
		}
	}
}
