using System;
using System.Collections.Generic;
using System.Text;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal abstract class InputDialogViewModel
		: ViewModelBase
	{
		public string Input
		{
			get => this.input;
			set
			{
				if (this.input == value)
					return;

				this.input = value;
				RaisePropertyChanged ();
				Command.RaiseCanExecuteChanged ();
			}
		}

		public bool IsBusy
		{
			get => this.isBusy;
			protected set
			{
				if (this.isBusy == value)
					return;

				this.isBusy = value;
				RaisePropertyChanged ();
				Command.RaiseCanExecuteChanged ();
			}
		}

		public string Error
		{
			get => this.error;
			protected set
			{
				if (this.error == value)
					return;

				this.error = value;
				RaisePropertyChanged ();
			}
		}

		protected virtual RelayCommand Command
		{
			get;
		}

		private string input, error;
		private bool isBusy;
	}
}
