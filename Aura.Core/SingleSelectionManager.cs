using System;
using System.Collections.Generic;
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
			Messenger.Default.Register<ElementsChangedMessage> (this, OnElementsChanged);
			SyncService = syncService ?? throw new ArgumentNullException (nameof (syncService));
			LoadElementsAsync ();
		}

		public Task Loading
		{
			get;
			private set;
		}

		public IReadOnlyList<T> Elements => this.elements;

		public T SelectedElement
		{
			get => this.selectedElement;
			set
			{
				if (this.selectedElement == value)
					return;

				SetElement (value);
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

			T existingElement = default;
			if (SelectedElement != default) {
				existingElement = items.FirstOrDefault (i => i.Id == SelectedElement.Id);
			}

			SelectedElement = existingElement ?? items.FirstOrDefault () ?? NoSelectionElement;
			this.elements.Reset (items);
		}

		private readonly ObservableCollectionEx<T> elements = new ObservableCollectionEx<T> ();
		private T selectedElement;

		private async void SetElement (T element)
		{
			var msg = new SingleSelectionPreviewChangeMessage (typeof (T), element);
			Messenger.Default.Send (msg);

			if (msg.Canceled != null) {
				if (await msg.Canceled) {
					Messenger.Default.Send (new SingleSelectionChangedMessage (typeof (T), this.selectedElement));
					return;
				}
			}

			this.selectedElement = element;
			OnPropertyChanged (nameof(SelectedElement));
			Messenger.Default.Send (new SingleSelectionChangedMessage (typeof (T), element));
		}

		private async void LoadElementsAsync ()
		{
			await (Loading = ReloadElementsAsync ());
		}

		private void OnElementsChanged (ElementsChangedMessage change)
		{
			if (typeof (T).IsAssignableFrom (change.Type))
				LoadElementsAsync ();
		}
	}
}
