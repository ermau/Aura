using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Moq;

namespace Aura.Tests
{
	internal class MockServiceProvider
		: IAsyncServiceProvider
	{
		public Mock<T> GetServiceMock<T>()
			where T : class
		{
			return (Mock<T>)(mocks.GetOrAdd (typeof (T), t => new Mock<T> ()));
		}

		public Task<T> GetServiceAsync<T> ()
		{
			if (this.services.TryGetValue (typeof (T), out object service))
				return Task.FromResult ((T)service);

			return Task.FromResult (GetValue<T> (this.mocks.GetOrAdd (typeof (T), t => CreateMock (t))));
		}

		public void AddService<T> (T service)
			where T : class
		{
			Mock<T> mock = Mock.Get (service);
			if (mock == null)
				this.services[typeof (T)] = service;
			else
				this.mocks[typeof (T)] = mock;
		}

		public Task<T[]> GetServicesAsync<T> ()
		{
			throw new NotImplementedException ();
		}

		private ConcurrentDictionary<Type, object> services = new ConcurrentDictionary<Type, object> ();
		private ConcurrentDictionary<Type, Mock> mocks = new ConcurrentDictionary<Type, Mock> ();

		private T GetValue<T> (Mock mock)
		{
			return (T)typeof (Mock<>)
				.MakeGenericType (typeof (T))
				.GetProperty (nameof (Mock<object>.Object))
				.GetGetMethod ().Invoke (mock, null);
		}

		private Mock CreateMock (Type type)
		{
			return (Mock)Activator.CreateInstance (typeof (Mock<>).MakeGenericType (type));
		}
	}
}
