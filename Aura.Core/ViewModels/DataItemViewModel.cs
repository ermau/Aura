using System.Threading.Tasks;
using System.Windows.Input;

using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal abstract class DataItemViewModel<T>
		: DataViewModel
	{
		protected DataItemViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService)
			: base (serviceProvider, syncService)
		{
			SaveCommand = new RelayCommand (OnSave, CanSave);
			ResetCommand = new RelayCommand (OnReset, CanReset);
		}

		public ICommand SaveCommand
		{
			get;
		}

		public ICommand ResetCommand
		{
			get;
		}

		public T Element
		{
			get => this.element;
			protected set
			{
				if (Equals (this.element, value))
					return;

				this.element = value;
				this.modifiedElement = value;
				RaisePropertyChanged ();
				RaisePropertyChanged (nameof (ModifiedElement));
			}
		}

		public T ModifiedElement
		{
			get => this.modifiedElement;
			protected set
			{
				if (Equals (this.modifiedElement, value))
					return;

				this.modifiedElement = value;
				RaisePropertyChanged ();
				OnModified ();
			}
		}

		protected virtual bool CanSave ()
			=> !Element.Equals (ModifiedElement);

		protected async void OnSave ()
		{
			await SaveAsync ();
		}

		protected async void Load ()
		{
			await LoadAsync ();
		}

		protected abstract Task LoadAsync ();
		protected abstract Task SaveAsync ();

		protected virtual void OnReset ()
		{
			ModifiedElement = Element;
		}

		protected virtual bool CanReset ()
			=> !Element.Equals (ModifiedElement);

		protected virtual void OnModified()
		{
			((RelayCommand)SaveCommand).RaiseCanExecuteChanged ();
			((RelayCommand)ResetCommand).RaiseCanExecuteChanged ();
		}

		private T element, modifiedElement;
	}
}
