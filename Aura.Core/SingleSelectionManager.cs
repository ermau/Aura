using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Aura.Data;
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

		protected async Task ReloadElementsAsync()
		{
			Task<string> selectionTask = null;
			string existingId = SelectedElement?.Id;
			if (existingId == null) {
				selectionTask = GetRecordedSelectionAsycnc ();
			}

			var items = (await SyncService.GetElementsAsync<T> ()).OrderBy (e => e.Name).ToList();

			T existingElement = default;
			if (selectionTask != null)
				existingId = await selectionTask;

			if (existingId != null)
				existingElement = items.FirstOrDefault (i => i.Id == existingId);

			SelectedElement = existingElement ?? items.FirstOrDefault () ?? NoSelectionElement;
			this.elements.Reset (items);
		}

		protected void NotifyAddElement (T element)
		{
			// TODO: Add with sort
			LoadElementsAsync ();
		}

		protected void NotifyRemoveElement (T element)
		{
			if (!this.elements.Remove (element)) {
				T existingElement = this.elements.FirstOrDefault (e => e.Id == element.Id);
				if (existingElement != null)
					this.elements.Remove (existingElement);
			}
		}

		private readonly ObservableCollectionEx<T> elements = new ObservableCollectionEx<T> ();
		private T selectedElement;
		private SingleSelectionElement selectionElement;

		private async Task<string> GetRecordedSelectionAsycnc()
		{
			var selections = await SyncService.GetElementsAsync<SingleSelectionElement> ();
			string typeName = typeof (T).GetSimpleTypeName ();
			this.selectionElement = selections.FirstOrDefault (s => s.Type == typeName);
			return this.selectionElement?.SelectionId;
		}

		private async Task SaveSelectionAsync()
		{
			SingleSelectionElement selection = this.selectionElement;
			if (selection == null) {
				selection = new SingleSelectionElement() {
					SelectionId = SelectedElement?.Id,
					Type = typeof(T).GetSimpleTypeName()
				};
			} else {
				selection = selection with { SelectionId = SelectedElement?.Id };
			}

			this.selectionElement = await SyncService.SaveElementAsync (selection);
		}

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
			await SaveSelectionAsync ();
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
