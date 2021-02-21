using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Aura.ViewModels;

using Moq;

using NUnit.Framework;

namespace Aura.Tests
{
	[TestFixture]
	public class DataItemViewModelTests
	{
		[Test]
		public void ElementSetsModified()
		{
			var vm = new ElementItemViewModel (new Mock<IAsyncServiceProvider> ().Object, new Mock<ISyncService> ().Object);

			Assume.That (vm.Element, Is.Null);
			Assume.That (vm.ModifiedElement, Is.Null);
			Assume.That (vm.SaveCommand, Is.Not.Null);

			var propertyChanges = new List<string> ();
			vm.PropertyChanged += (o, e) => {
				propertyChanges.Add (e.PropertyName);
			};

			var element = new Element ();
			vm.SetElement (element);

			Assert.That (vm.Element, Is.EqualTo (element));
			Assert.That (vm.ModifiedElement, Is.EqualTo (element));
			Assert.That (propertyChanges, Contains.Item (nameof (ElementItemViewModel.Element)));
			Assert.That (propertyChanges, Contains.Item (nameof (ElementItemViewModel.ModifiedElement)));
			Assert.That (vm.SaveCommand.CanExecute (null), Is.False);
		}

		[Test]
		public void ModifyingEnablesSave()
		{
			var vm = new ElementItemViewModel (new Mock<IAsyncServiceProvider> ().Object, new Mock<ISyncService> ().Object);
			vm.SetElement (new Element { Content = "content" });
			Assume.That (vm.SaveCommand, Is.Not.Null);

			bool raisedExecuteChange = false;
			vm.ResetCommand.CanExecuteChanged += (o, e) => raisedExecuteChange = true;

			var propertyChanges = new List<string> ();
			vm.PropertyChanged += (o, e) => {
				propertyChanges.Add (e.PropertyName);
			};

			vm.SetModifiedElement (vm.ModifiedElement with { Content = "new content" });
			Assert.That (raisedExecuteChange, Is.True);
			Assert.That (propertyChanges, Contains.Item (nameof (ElementItemViewModel.ModifiedElement)));
			Assert.That (propertyChanges, Does.Not.Contain (nameof (ElementItemViewModel.Element)));
			Assert.That (vm.SaveCommand.CanExecute (null), Is.True);
		}

		[Test]
		public void ModifyingEnablesReset()
		{
			var vm = new ElementItemViewModel (new Mock<IAsyncServiceProvider> ().Object, new Mock<ISyncService> ().Object);
			vm.SetElement (new Element { Content = "content" });
			Assume.That (vm.ResetCommand, Is.Not.Null);

			bool raisedExecuteChange = false;
			vm.ResetCommand.CanExecuteChanged += (o, e) => raisedExecuteChange = true;

			var propertyChanges = new List<string> ();
			vm.PropertyChanged += (o, e) => {
				propertyChanges.Add (e.PropertyName);
			};

			vm.SetModifiedElement (vm.ModifiedElement with { Content = "new content" });
			Assert.That (raisedExecuteChange, Is.True);
			Assert.That (propertyChanges, Contains.Item (nameof (ElementItemViewModel.ModifiedElement)));
			Assert.That (propertyChanges, Does.Not.Contain (nameof (ElementItemViewModel.Element)));
			Assert.That (vm.ResetCommand.CanExecute (null), Is.True);
		}

		[Test]
		public void Reset()
		{
			var vm = new ElementItemViewModel (new Mock<IAsyncServiceProvider> ().Object, new Mock<ISyncService> ().Object);
			vm.SetElement (new Element { Content = "content" });
			Assume.That (vm.ResetCommand, Is.Not.Null);

			bool raisedExecuteChange = false;
			vm.ResetCommand.CanExecuteChanged += (o, e) => raisedExecuteChange = true;

			vm.SetModifiedElement (vm.ModifiedElement with { Content = "new content" });
			Assume.That (vm.Element.Content, Is.EqualTo ("content"));
			Assume.That (raisedExecuteChange, Is.True);
			Assume.That (vm.ResetCommand.CanExecute (null), Is.True);

			var propertyChanges = new List<string> ();
			vm.PropertyChanged += (o, e) => {
				propertyChanges.Add (e.PropertyName);
			};

			vm.ResetCommand.Execute (null);
			Assert.That (propertyChanges, Contains.Item (nameof (ElementItemViewModel.ModifiedElement)));
			Assert.That (propertyChanges, Does.Not.Contain (nameof (ElementItemViewModel.Element)));
			Assert.That (vm.ModifiedElement, Is.EqualTo (vm.Element));
		}

		private record Element
		{
			public string Content
			{
				get;
				init;
			}
		}

		private class ElementItemViewModel
			: DataItemViewModel<Element>
		{
			public ElementItemViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService)
				: base (serviceProvider, syncService)
			{
			}

			public void SetElement (Element element)
			{
				Element = element;
			}

			public void SetModifiedElement (Element element)
			{
				ModifiedElement = element;
			}

			protected override Task LoadAsync ()
			{
				return Task.CompletedTask;
			}

			protected override Task SaveAsync ()
			{
				return Task.CompletedTask;
			}
		}
	}
}
