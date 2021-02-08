using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Aura.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class PairServiceViewModel
		: ViewModelBase
	{
		public PairServiceViewModel (IPairedService service)
		{
			Service = service;
			PairCommand = new RelayCommand<PairingOption> (OnPair, OnCanPair);
			CancelCommand = new RelayCommand (Cancel);
			RefreshCommand = new RelayCommand (Refresh, () => !IsBusy);
			Refresh ();
		}

		public Task<(PairingOption, string)> PairResult => this.tcs.Task;

		// TODO: localize
		public string Title => $"Pair to {Service.PairedDeviceName}";

		public bool IsBusy
		{
			get => this.isBusy;
			private set
			{
				if (this.isBusy == value)
					return;

				this.isBusy = value;
				RaisePropertyChanged ();
				((RelayCommand)RefreshCommand).RaiseCanExecuteChanged ();
			}
		}

		public IReadOnlyList<PairingOption> Options => this.options;

		public ICommand PairCommand
		{
			get;
		}

		public ICommand CancelCommand
		{
			get;
		}

		public ICommand RefreshCommand
		{
			get;
		}

		public void Cancel()
		{
			this.cancelPair.Cancel ();
			this.tcs.TrySetCanceled ();
		}

		public IPairedService Service { get; }

		private bool isBusy = true;
		private readonly ObservableCollectionEx<PairingOption> options = new ObservableCollectionEx<PairingOption> ();
		private readonly CancellationTokenSource cancelPair = new CancellationTokenSource();
		private readonly TaskCompletionSource<(PairingOption, string)> tcs = new TaskCompletionSource<(PairingOption, string)> ();

		private async void Refresh()
		{
			await LoadAsync ();
		}

		private async Task LoadAsync()
		{
			IsBusy = true;
			try {
				this.options.Reset (await Service.GetPairingOptionsAsync (CancellationToken.None));
			} catch (OperationCanceledException) {
				this.options.Clear ();
			} finally {
				IsBusy = false;
			}
		}

		private async void OnPair (PairingOption option)
		{
			if (option == null)
				return;

			if (Service.WaitsForUser)
				MessengerInstance.Send (new PairServiceWaitMessage (this));
			
			try {
				string key = await Service.PairAsync (option.Id, cancelPair.Token);
				this.tcs.TrySetResult ((option, key));
			} catch (OperationCanceledException) {
				this.tcs.TrySetCanceled ();
			} catch (Exception ex) {
				this.tcs.TrySetException (ex);
			}
		}

		private bool OnCanPair (PairingOption option)
			=> option != null;
	}
}