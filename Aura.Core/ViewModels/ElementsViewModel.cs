using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aura.Data;

namespace Aura.ViewModels
{
	internal abstract class ElementsViewModel<T, TViewModel>
		: DataViewModel
		where T : Element
		where TViewModel : ElementViewModel<T>
	{
		public ElementsViewModel (IAsyncServiceProvider services)
			: base (services)
		{
		}

		public IReadOnlyList<TViewModel> Elements => this.elements;

		public TViewModel SelectedElement
		{
			get => this.selectedElement;
			set
			{
				if (this.selectedElement == value)
					return;

				this.selectedElement = value;
				RaisePropertyChanged ();
			}
		}

		protected abstract TViewModel CreateElementViewModel (T element);

		protected abstract T InitializeElement (string name);

		protected async Task LoadAsync()
		{
			this.elements.Reset (await LoadElementsAsync ());
		}

		protected virtual async Task<IReadOnlyList<TViewModel>> LoadElementsAsync ()
		{
			await SetupTask;
			var elements = await SyncService.GetElementsAsync<T> ();
			return elements.Select (e => CreateElementViewModel (e)).ToList ();
		}

		private readonly ObservableCollectionEx<TViewModel> elements = new ObservableCollectionEx<TViewModel> ();
		private TViewModel selectedElement;
	}
}
