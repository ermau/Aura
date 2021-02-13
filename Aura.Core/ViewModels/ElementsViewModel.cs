using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Aura.Data;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal abstract class ElementsViewModel<T, TViewModel>
		: DataViewModel
		where T : NamedElement
		where TViewModel : ElementViewModel<T>
	{
		public ElementsViewModel (IAsyncServiceProvider services)
			: base (services)
		{
			DeleteCommand = new RelayCommand (OnDelete, CanDelete);
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
				((RelayCommand)DeleteCommand).RaiseCanExecuteChanged ();
			}
		}

		public ICommand DeleteCommand
		{
			get;
		}

		public async void RequestSelection (string id)
		{
			if (string.IsNullOrWhiteSpace (id))
				throw new ArgumentException ($"'{nameof (id)}' cannot be null or whitespace", nameof (id));

			await this.loadingTask;
			SelectedElement = this.elements.FirstOrDefault (vm => vm.Element.Id.Equals (id));
		}

		protected virtual async Task<TViewModel> CreateElementAsync (string name)
		{
			T element = InitializeElement (name);
			element = await SyncService.SaveElementAsync (element);
			TViewModel vm = InitializeElementViewModel (element);
			this.elements.Add (vm);
			return vm;
		}

		protected abstract TViewModel InitializeElementViewModel (T element);

		protected abstract T InitializeElement (string name);

		protected async Task LoadAsync()
		{
			this.elements.Reset (await LoadElementsAsync ());
		}

		protected virtual async Task<IReadOnlyList<TViewModel>> LoadElementsAsync ()
		{
			await SetupTask;
			var elements = await SyncService.GetElementsAsync<T> ();
			return elements.Select (e => InitializeElementViewModel (e)).ToList ();
		}

		protected async void RequestReload()
		{
			this.loadingTask = ReloadAsync ();
			await this.loadingTask;
		}

		private readonly ObservableCollectionEx<TViewModel> elements = new ObservableCollectionEx<TViewModel> ();
		private TViewModel selectedElement;
		private string selectedId;
		private Task loadingTask;

		private void OnDelete()
		{
			SelectedElement.DeleteCommand.Execute (null);
			RequestReload ();
		}

		private bool CanDelete () => SelectedElement != null;

		private async Task ReloadAsync()
		{
			await SetupTask;
			this.selectedId = this.selectedElement?.Element.Id;
			await LoadAsync ();
			SelectedElement = (this.selectedId != null)
				? this.elements.FirstOrDefault (vm => vm.Element.Id.Equals (this.selectedId))
				: null;
		}
	}
}
