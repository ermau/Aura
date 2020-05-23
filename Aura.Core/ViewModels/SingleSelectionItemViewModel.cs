using System;
using System.Collections.Generic;
using System.Text;

using Aura.Messages;

using GalaSoft.MvvmLight;

namespace Aura.ViewModels
{
	internal class SingleSelectionItemViewModel<T>
		: ViewModelBase
		where T : NamedElement
	{
		public SingleSelectionItemViewModel (SingleSelectionManager<T> manager, T element)
		{
			this.manager = manager ?? throw new ArgumentNullException (nameof (manager));
			this.element = element ?? throw new ArgumentNullException (nameof (element));

			MessengerInstance.Register<SingleSelectionChangedMessage> (this, OnSelectionChanged);
			UpdateIsSelected ();
		}

		public string Name => this.element.Name;

		public bool IsSelected
		{
			get => this.isSelected;
			set
			{
				if (this.isSelected == value)
					return;

				this.manager.SelectedElement = this.element;
			}
		}

		private readonly SingleSelectionManager<T> manager;
		private bool isSelected;
		private T element;

		private void UpdateIsSelected () => this.isSelected = this.element.Id == this.manager.SelectedElement?.Id;

		private void OnSelectionChanged (SingleSelectionChangedMessage msg)
		{
			if (msg.Type != typeof (T))
				return;

			IsSelected = msg.SelectedElement == this.element;
		}
	}
}
