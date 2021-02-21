using System.Collections.Specialized;
using System.Threading.Tasks;

using Aura.Data;

namespace Aura.ViewModels
{
	internal abstract class EditSingleSelectionItemsViewModel<T, TViewModel>
		: ElementsViewModel<T, EditSingleSelectionElementViewModel<T>>
		where T : NamedElement
		where TViewModel : EditSingleSelectionElementViewModel<T>
	{
		public EditSingleSelectionItemsViewModel (IAsyncServiceProvider services)
			: base (services)
		{
			RequestReload ();
		}

		protected SingleSelectionManager<T> Manager
		{
			get;
			private set;
		}

		protected abstract Task<SingleSelectionManager<T>> GetManagerAsync ();

		protected override async Task SetupAsync ()
		{
			await base.SetupAsync ();
			Manager = await GetManagerAsync ();
			((INotifyCollectionChanged)Manager.Elements).CollectionChanged += OnManagerElementsChanged;
		}

		private void OnManagerElementsChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			RequestReload ();
		}
	}

	internal class EditSingleSelectionElementViewModel<T>
		: ElementViewModel<T>
		where T : NamedElement
	{
		public EditSingleSelectionElementViewModel (IAsyncServiceProvider services, ISyncService sync, T element)
			: base (services, sync, element)
		{
		}
	}
}
