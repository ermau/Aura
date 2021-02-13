using System;
using System.Threading.Tasks;
using System.Windows.Input;

using GalaSoft.MvvmLight.Command;

using Aura.Data;

namespace Aura.ViewModels
{
	internal abstract class ElementViewModel<T>
		: DataViewModel
		where T : Element
	{
		protected ElementViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService, string id)
			: this (serviceProvider, syncService)
		{
			if (id is null)
				throw new ArgumentNullException (nameof (id));

			this.id = id;
			Load ();
		}

		protected ElementViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService, T element)
			: this (serviceProvider, syncService)
		{
			this.id = element.Id;
			Element = element;
			ModifiedElement = Element;
			Load ();
		}

		private ElementViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService)
			: base (serviceProvider, syncService)
		{
			DeleteCommand = new RelayCommand (OnDelete, CanDelete);
			SaveCommand = new RelayCommand (OnSave, CanSave);
			ResetCommand = new RelayCommand (OnReset, CanReset);
		}

		public ICommand DeleteCommand
		{
			get;
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
				if (this.element == value)
					return;

				this.element = value;
				RaisePropertyChanged ();
			}
		}

		protected T ModifiedElement
		{
			get => this.modifiedElement;
			set
			{
				if (this.modifiedElement == value)
					return;

				this.modifiedElement = value;
				RaisePropertyChanged ();
				OnModified ();
			}
		}

		protected async void Load()
		{
			await LoadAsync ();
		}

		protected virtual async Task LoadAsync ()
		{
			await SetupTask;

			if (Element == default) {
				Element = await SyncService.GetElementByIdAsync<T> (this.id);
				ModifiedElement = Element;
			}
		}

		protected virtual void OnModified()
		{
			((RelayCommand)SaveCommand).RaiseCanExecuteChanged ();
			((RelayCommand)ResetCommand).RaiseCanExecuteChanged ();
		}

		protected virtual bool CanDelete() => true;

		protected virtual async void OnDelete()
		{
			try {
				await SetupTask;
				await SyncService.DeleteElementAsync (Element);
			} catch (OperationCanceledException) {
			}
		}

		protected virtual bool CanSave()
			=> !Element.Equals (ModifiedElement);

		protected async void OnSave()
		{
			await SaveAsync ();
			Load ();
		}

		protected virtual async Task SaveAsync()
		{
			AddWork ();
			
			await SetupTask;
			try {
				Element = await SyncService.SaveElementAsync (ModifiedElement);
				ModifiedElement = Element;
			} finally {
				FinishWork ();
			}
		}

		protected virtual void OnReset()
		{
			ModifiedElement = Element;
		}

		protected virtual bool CanReset ()
			=> !Element.Equals (ModifiedElement);

		private readonly string id;
		private T element, modifiedElement;
	}
}
