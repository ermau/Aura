using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal abstract class PromptRequestViewModel
		: ViewModelBase
	{
		protected PromptRequestViewModel()
		{
			CancelCommand = new RelayCommand (() => IsOpen = false);
		}

		public abstract string Message { get; }

		public bool IsOpen
		{
			get => this.isOpen;
			set
			{
				if (this.isOpen == value)
					return;

				this.isOpen = value;
				RaisePropertyChanged ();
			}
		}

		public ICommand CancelCommand
		{
			get;
		}

		private bool isOpen = true;
	}
}
