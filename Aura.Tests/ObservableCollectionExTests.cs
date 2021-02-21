using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Aura.Tests
{
	[TestFixture]
	public class ObservableCollectionExTests
	{
		[Test]
		public void UpdateBasedOnId()
		{
			var a = new TestElement ("0", "A");
			var c = new ObservableCollectionEx<TestElement> {
				a,
				new TestElement ("1", "C")
			};

			var source = new[] { new TestElement ("2", "B"), new TestElement ("0", "A") };

			c.Update(source.Select (te => te.Id),
				te => te.Id,
				id => source.Single(te => te.Id == id));
			
			Assert.That(c.Count, Is.EqualTo (2));
			Assert.That(c[0], Is.SameAs (source[0]));
			Assert.That (c[1], Is.SameAs (a));
		}

		[Test]
		public void UpdateBasedOnElements ()
		{
			var a = new TestElement ("0", "A");
			var c = new ObservableCollectionEx<TestElement> {
				a,
				new TestElement ("1", "C")
			};

			var source = new[] { new TestElement ("2", "B"), new TestElement ("0", "A") };

			c.Update (source, te => te.Id);

			Assert.That (c.Count, Is.EqualTo (2));
			Assert.That (c[0], Is.SameAs (source[0]));
			Assert.That (c[1], Is.SameAs (a));
		}

		private class TestElement
		{
			public TestElement (string id, string name)
			{
				Id = id;
				Name = name;
			}

			public string Id
			{
				get;
				set;
			}

			public string Name
			{
				get;
				set;
			}

			public override string ToString () => $"{GetType().Name} ({Id}:{Name})";
		}
	}
}
