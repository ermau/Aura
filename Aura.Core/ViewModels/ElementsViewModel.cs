using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Aura.Data;
using Aura.Messages;
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
			CreateCommand = new RelayCommand<string> (OnCreate, CanCreate);

			MessengerInstance.Register<ElementsChangedMessage> (this, OnElementsChanged);
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

		public ICommand CreateCommand
		{
			get;
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
			this.saving = true;
			element = await SyncService.SaveElementAsync (element);
			this.saving = false;
			TViewModel vm = InitializeElementViewModel (element);
			this.elements.Add (vm);
			return vm;
		}

		protected abstract TViewModel InitializeElementViewModel (T element);

		protected abstract T InitializeElement (string name);

		protected async Task LoadAsync()
		{
			var elements = (await LoadElementsAsync ()).ToDictionary (t => t.Id);
			var ordered = elements.Values.OrderBy (t => t.Name).Select (t => t.Id);
			this.elements.Update (ordered, vm => vm.Id, id => InitializeElementViewModel (elements[id]));
		}

		protected virtual async Task<IReadOnlyList<T>> LoadElementsAsync ()
		{
			await SetupTask;
			return await SyncService.GetElementsAsync<T> ();
		}

		protected virtual bool CanCreate (string input) =>
			!String.IsNullOrWhiteSpace (input);

		protected virtual async void OnCreate (string input)
		{
			SelectedElement = await CreateElementAsync (input);
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
		private bool saving;

		private void OnDelete()
		{
			SelectedElement.DeleteCommand.Execute (null);
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

		private void OnElementsChanged (ElementsChangedMessage msg)
		{
			if (this.saving || !typeof (T).IsAssignableFrom (msg.Type))
				return;

			SynchronizationContext.Post (s => RequestReload (), null);
		}
	}
}
