using System.Collections.Generic;

using Newtonsoft.Json;

namespace Aura.FreeSound.API
{
	public abstract class FreeSoundPagedResponse<T>
	{
		[JsonProperty ("next")]
		public string NextUrl
		{
			get;
			set;
		}

		public IReadOnlyList<T> Results
		{
			get;
			set;
		}

		[JsonProperty ("previous")]
		public string PreviousUrl
		{
			get;
			set;
		}
	}
}
