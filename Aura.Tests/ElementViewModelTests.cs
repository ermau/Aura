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
	public class ElementViewModelTests
	{
		[Test]
		public void ModifiedElementMatches()
		{
			var element = new NamedElement { Name = "new item" };
			var vm = new TestElementViewModel (new Mock<IAsyncServiceProvider> ().Object, new Mock<ISyncService>().Object, element);

			Assert.That (vm.Element, Is.EqualTo (element));
			Assert.That (vm.ModifiedElement, Is.EqualTo (element));
		}

		[Test]
		public void SaveEnabledOnNewItem()
		{
			var sync = new Mock<ISyncService> ();
			sync.Setup (s => s.SaveElementAsync (It.IsAny<NamedElement> ()))
				.Returns<NamedElement> (ne => Task.FromResult (ne));

			var element = new NamedElement { Name = "new item" };
			var vm = new TestElementViewModel (new Mock<IAsyncServiceProvider> ().Object, sync.Object, element);

			Assert.That (vm.SaveCommand.CanExecute (null), Is.True);
		}

		[Test]
		public void Save()
		{
			var sync = new Mock<ISyncService> ();
			sync.Setup (s => s.SaveElementAsync (It.IsAny<NamedElement> ()))
				.Returns<NamedElement> (ne => Task.FromResult (ne));

			var element = new NamedElement { Id = Guid.NewGuid().ToString(), Name = "existing item" };
			var vm = new TestElementViewModel (new Mock<IAsyncServiceProvider> ().Object, sync.Object, element);

			Assume.That (vm.Element, Is.EqualTo (vm.ModifiedElement));
			vm.SetModified (vm.ModifiedElement with { Name = "new name" });
			Assume.That (vm.SaveCommand.CanExecute (null), Is.True);
			Assume.That (vm.ResetCommand.CanExecute (null), Is.True);

			var propertyChanges = new List<string> ();
			vm.PropertyChanged += (o, e) => {
				propertyChanges.Add (e.PropertyName);
			};

			bool saveCanExecuteChanged = false, resetCanExecuteChanged = false;
			vm.SaveCommand.CanExecuteChanged += (o, e) => saveCanExecuteChanged = true;
			vm.ResetCommand.CanExecuteChanged += (o, e) => resetCanExecuteChanged = true;
			vm.SaveCommand.Execute (null);

			Assert.That (saveCanExecuteChanged, Is.True);
			Assert.That (vm.SaveCommand.CanExecute (null), Is.False);
			Assert.That (resetCanExecuteChanged, Is.True);
			Assume.That (vm.ResetCommand.CanExecute (null), Is.False);
			Assert.That (propertyChanges, Contains.Item (nameof (TestElementViewModel.Element)));
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
