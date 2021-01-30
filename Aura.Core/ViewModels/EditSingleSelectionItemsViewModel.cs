using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Aura.Data;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal abstract class EditSingleSelectionItemsViewModel<T, TViewModel>
		: ViewModelBase
		where T : NamedElement
		where TViewModel : EditSingleSelectionElementViewModel<T>
	{
		public EditSingleSelectionItemsViewModel (IAsyncServiceProvider services)
		{
			Services = services ?? throw new ArgumentNullException (nameof (services));
			LoadAsync ();
		}

		public IReadOnlyList<TViewModel> Elements
		{
			get;
			private set;
		}

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

		protected SingleSelectionManager<T> Manager
		{
			get;
			private set;
		}

		protected IAsyncServiceProvider Services
		{
			get;
		}

		protected abstract Task<SingleSelectionManager<T>> GetManagerAsync ();

		protected abstract TViewModel CreateViewModel (IAsyncServiceProvider services, T element);

		private TViewModel selectedElement;

		private async void LoadAsync ()
		{
			Manager = await GetManagerAsync ();
			((INotifyCollectionChanged)Manager.Elements).CollectionChanged += OnManagerElementsChanged;
			OnManagerElementsChanged (Manager, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
		}

		private void OnManagerElementsChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			Elements = Manager.Elements.Select (c => CreateViewModel (Services, c)).ToArray ();
			SelectedElement = default(TViewModel);
			RaisePropertyChanged (nameof (Elements));

			string id = SelectedElement?.Id;
			if (id != null)
				SelectedElement = Elements.SingleOrDefault (c => c.Id == id);
		}
	}

	internal class EditSingleSelectionElementViewModel<T>
		: ViewModelBase
		where T : NamedElement
	{
		public EditSingleSelectionElementViewModel (IAsyncServiceProvider services, T element)
		{
			this.services = services ?? throw new ArgumentNullException (nameof(services));
			this.element = element ?? throw new ArgumentNullException (nameof(element));

			DeleteCommand = new RelayCommand (OnDelete);
		}

		public string Id => this.element.Id;
		public string Name => this.element.Name;

		public T Element => this.element;

		public ICommand DeleteCommand
		{
			get;
		}

		private readonly T element;
		private readonly IAsyncServiceProvider services;

		private async void OnDelete()
		{
			var sync = await this.services.GetServiceAsync<ISyncService> ();
			await sync.DeleteElementAsync (this.element);
		}
	}
}
