using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;

using System.Threading;
using System.Threading.Tasks;

namespace Aura.Services
{
	internal class AsyncServiceProvider
		: IAsyncServiceProvider
	{
		public AsyncServiceProvider (params Assembly[] assemblies)
		{
			compositionHost = GetCompositionHostAsync (assemblies);
		}

		public void Expect<T>()
		{
			this.services[typeof (T)] = new Expected<T> ().Source.Task;
		}

		public void Expect<T> (Task<T> pending)
		{
			this.services[typeof (T)] = pending;
		}

		public void Register<T> (T instance)
		{
			if (this.services.TryGetValue (typeof(T), out Task result)) {
				var expected = (Expected<T>)result.AsyncState;
				expected.Source.SetResult (instance);
			} else
				this.services[typeof (T)] = Task.FromResult (instance);
		}

		public async Task<T[]> GetServicesAsync<T>()
		{
			var host = await this.compositionHost.ConfigureAwait (false);
			return host.GetExports<T> ().ToArray ();
		}

		public async Task<T> GetServiceAsync<T> ()
		{
			if (this.services.TryGetValue (typeof (T), out Task existing))
				return await ((Task<T>)existing).ConfigureAwait (false);

			var host = await this.compositionHost.ConfigureAwait (false);
			T instance = host.GetExport<T> ();

			this.services[typeof (T)] = Task.FromResult<T> (instance);
			return instance;
		}

		private readonly Task<CompositionHost> compositionHost;
		private readonly ConcurrentDictionary<Type, Task> services = new ConcurrentDictionary<Type, Task> ();

		private Task<CompositionHost> GetCompositionHostAsync (Assembly[] assemblies)
		{
			return Task.Run (() => {
				ContainerConfiguration configuration = new ContainerConfiguration ();
				return configuration.WithAssemblies (assemblies).CreateContainer ();
			});
		}

		private class Expected<T>
		{
			public Expected()
			{
				Source = new TaskCompletionSource<T> (this);
			}

			public TaskCompletionSource<T> Source
			{
				get;
			}
		}
	}
}
