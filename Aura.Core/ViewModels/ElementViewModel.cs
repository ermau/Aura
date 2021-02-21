using System;
using System.Threading.Tasks;
using System.Windows.Input;

using GalaSoft.MvvmLight.Command;

using Aura.Data;
using Aura.Messages;

namespace Aura.ViewModels
{
	internal abstract class ElementViewModel<T>
		: DataItemViewModel<T>
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

			MessengerInstance.Register<ElementsChangedMessage> (this, OnElementsChanged);
		}

		public ICommand DeleteCommand
		{
			get;
		}

		public string Id => this.id;

		protected override async Task LoadAsync ()
		{
			await SetupTask;

			if (Element == default) {
				Element = await SyncService.GetElementByIdAsync<T> (this.id);
				ModifiedElement = Element;
			}
		}

		protected override bool CanSave ()
		{
			return base.CanSave () || (Element != null && Element.Id == null);
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

		protected override async Task SaveAsync()
		{
			AddWork ();
			
			await SetupTask;
			try {
				Element = await SyncService.SaveElementAsync (ModifiedElement);
				((RelayCommand)SaveCommand).RaiseCanExecuteChanged ();
				((RelayCommand)ResetCommand).RaiseCanExecuteChanged ();
			} finally {
				FinishWork ();
			}
		}

		private readonly string id;

		private async void OnElementsChanged (ElementsChangedMessage msg)
		{
			if (!typeof (T).IsAssignableFrom (msg.Type) || this.id != msg.Id)
				return;

			Element = await SyncService.GetElementByIdAsync<T> (this.id);
			if (Element == null)
				return;

			ModifiedElement = Element;
		}
	}
}
