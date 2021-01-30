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
			: base (serviceProvider, syncService)
		{
			if (id is null)
				throw new ArgumentNullException (nameof (id));

			DeleteCommand = new RelayCommand (OnDelete, CanDelete);
		}

		protected ElementViewModel (IAsyncServiceProvider serviceProvider, ISyncService syncService, T element)
			: base (serviceProvider, syncService)
		{
			this.id = element.Id;
			Element = element;
		}

		public ICommand DeleteCommand
		{
			get;
		}

		public T Element
		{
			get;
			private set;
		}

		protected override async Task SetupAsync ()
		{
			await base.SetupAsync ();
			if (Element == default) {
				Element = await SyncService.GetElementByIdAsync<T> (this.id);
			}
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

		private readonly string id;
	}
}
