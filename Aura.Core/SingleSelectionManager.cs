using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Aura.Messages;

using GalaSoft.MvvmLight.Messaging;

namespace Aura
{
	internal class SingleSelectionManager<T>
		: NotifyingObject
		where T : NamedElement
	{
		public SingleSelectionManager (ISyncService syncService)
		{
			SyncService = syncService ?? throw new ArgumentNullException (nameof (syncService));
			LoadElementsAsync ();
		}

		public IReadOnlyList<T> Elements => this.elements;

		public T SelectedElement
		{
			get => this.selectedElement;
			set
			{
				if (this.selectedElement == value)
					return;

				this.selectedElement = value;
				OnPropertyChanged ();
				Messenger.Default.Send (new SingleSelectionChangedMessage (typeof (T), value));
			}
		}

		protected virtual T NoSelectionElement => default;

		protected ISyncService SyncService
		{
			get;
		}

		protected void AddElement (T element)
		{
			// TODO: Add with sort
			LoadElementsAsync ();
		}

		protected void RemoveElement (T element)
		{
			if (!this.elements.Remove (element)) {
				T existingElement = this.elements.FirstOrDefault (e => e.Id == element.Id);
				if (existingElement != null)
					this.elements.Remove (existingElement);
			}
		}

		protected async Task ReloadElementsAsync()
		{
			var items = (await SyncService.GetElementsAsync<T> ()).OrderBy (e => e.Name).ToList();

			SelectedElement = items?.FirstOrDefault () ?? NoSelectionElement;
			this.elements.Reset (items);
		}

		private readonly ObservableCollectionEx<T> elements = new ObservableCollectionEx<T> ();
		private T selectedElement;

		private async void LoadElementsAsync ()
		{
			await ReloadElementsAsync ();
		}
	}
}
