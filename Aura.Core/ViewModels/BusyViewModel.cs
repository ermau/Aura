using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using GalaSoft.MvvmLight;

namespace Aura.ViewModels
{
	internal class BusyViewModel
		: ViewModelBase
	{
		protected BusyViewModel()
		{
			this.synchronizationContext = SynchronizationContext.Current;
		}

		public bool IsBusy
		{
			get => this.isBusy;
			private set
			{
				if (this.isBusy == value)
					return;

				this.isBusy = value;
				RaisePropertyChanged ();
			}
		}

		protected void AddWork()
		{
			if (Interlocked.Increment (ref this.operationCount) == 1)
				PushUpdate ();
		}

		protected void FinishWork()
		{
			if (Interlocked.Decrement (ref this.operationCount) == 0)
				PushUpdate ();
		}

		protected SynchronizationContext SynchronizationContext => this.synchronizationContext;

		private readonly SynchronizationContext synchronizationContext;
		private int operationCount;
		private bool isBusy;

		private void PushUpdate()
		{
			if (this.synchronizationContext == null)
				UpdateBusyState ();
			else
				this.synchronizationContext.Post ((s) => UpdateBusyState (), null);
		}

		private void UpdateBusyState()
		{
			IsBusy = this.operationCount > 0;
		}
	}
}
