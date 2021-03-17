using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Aura.Data;

using Newtonsoft.Json;

namespace Aura.Tests
{
	internal class MockSyncService
		: JsonSyncServiceBase
	{
		protected override Task<IDictionary<string, IDictionary<string, object>>> LoadAsync ()
		{
			var serializer = new JsonSerializer {
				TypeNameHandling = TypeNameHandling.Auto
			};

			var result = (IDictionary<string, IDictionary<string, object>>)
				serializer.Deserialize (new StringReader (this.db), typeof (IDictionary<string, IDictionary<string, object>>));

			return Task.FromResult (result);
		}

		protected override Task SaveAsync (IDictionary<string, IDictionary<string, object>> data)
		{
			Interlocked.Exchange (ref this.db, JsonConvert.SerializeObject (data, new JsonSerializerSettings {
				TypeNameHandling = TypeNameHandling.Auto,
				Formatting = Formatting.Indented
			}));

			return Task.CompletedTask;
		}

		private string db = "{}";
	}
}
