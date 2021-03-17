using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aura.Services;

using Moq;

namespace Aura.Tests
{
	internal static class TestExtensions
	{
		public static Mock<T> MockService<T> (this AsyncServiceProvider self)
			where T : class
		{
			var mock = new Mock<T> ();
			self.Register<T> (mock.Object);
			return mock;
		}
	}
}
