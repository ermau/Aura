using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aura.Data;
using Aura.ViewModels;

using Moq;

using NUnit.Framework;

namespace Aura.Tests
{
	[TestFixture]
	public class ElementsViewModelTests
	{
		[Test]
		public void SelectAfterSave()
		{
			var vm = new TestElementsViewModel (this.services.Object);

			string input = "new item";
			Assume.That (vm.CreateCommand.CanExecute (input), Is.True);
			Assume.That (vm.SelectedElement, Is.Null);

			var propertyChanges = new List<string> ();
			vm.PropertyChanged += (o, e) => {
				propertyChanges.Add (e.PropertyName);
			};

			vm.CreateCommand.Execute (input);

			Assert.That (vm.SelectedElement, Is.Not.Null);
			Assert.That (vm.SelectedElement.Element.Name, Is.EqualTo (input));
			Assert.That (propertyChanges, Contains.Item (nameof (TestElementsViewModel.SelectedElement)));
		}

		[OneTimeSetUp]
		public void SetupOnce()
		{
			this.sync.Setup (s => s.SaveElementAsync (It.IsAny<NamedElement> ()))
				.Returns<NamedElement> (ne => {
					if (ne.Id == null) {
						ne = ne with { Id = Guid.NewGuid ().ToString () };
						this.items.Add (ne);
					} else {
						int index = this.items.FindIndex (ee => ee.Id == ne.Id);
						this.items[index] = ne;
					}

					return Task.FromResult (ne);
				});

			this.sync.Setup (s => s.GetElementsAsync<NamedElement> ())
				.Returns (() => Task.FromResult<IReadOnlyList<NamedElement>> (this.items.ToList ()));

			this.services.Setup (s => s.GetServiceAsync<ISyncService> ())
				.Returns (Task.FromResult (this.sync.Object));
		}

		[SetUp]
		public void Setup()
		{
			this.items.Clear ();
		}

		private readonly List<NamedElement> items = new List<NamedElement> ();
		private Mock<ISyncService> sync = new Mock<ISyncService> ();
		private Mock<IAsyncServiceProvider> services = new Mock<IAsyncServiceProvider> ();

		private class TestElementsViewModel
			: ElementsViewModel<NamedElement, TestElementViewModel>
		{
			public TestElementsViewModel (IAsyncServiceProvider serviceProvider)
				: base (serviceProvider)
			{
			}

			protected override NamedElement InitializeElement (string name)
			{
				return new NamedElement { Name = name };
			}

			protected override TestElementViewModel InitializeElementViewModel (NamedElement element)
			{
				return new TestElementViewModel (ServiceProvider, SyncService, element);
			}
		}


		private class TestElementViewModel
			: ElementViewModel<NamedElement>
		{
			public TestElementViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService, NamedElement element)
				: base (serviceProvider, syncService, element)
			{
			}

			public void SetModified (NamedElement element)
			{
				ModifiedElement = element;
			}
		}
	}
}
