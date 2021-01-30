using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Aura.Data;
using Aura.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal abstract class CampaignElementsViewModel<T, TViewModel>
		: CampaignViewModel
		where T : CampaignChildElement
		where TViewModel : ElementViewModel<T>
	{
		protected CampaignElementsViewModel (IAsyncServiceProvider services)
			: base (services)
		{
			MessengerInstance.Register<ElementsChangedMessage> (this, OnElementsChanged);

			OnCampaignChanged ();
			CreateElementCommand = new RelayCommand<string> (async s => {
				TViewModel vm = await CreateElementAsync (s);
				SelectedElement = vm;
			}, s => !String.IsNullOrWhiteSpace (s));
		}

		public IReadOnlyList<TViewModel> Elements => this.elements;

		public TViewModel SelectedElement
		{
			get => this.selectedElement;
			set
			{
				if (this.selectedElement == value)
					return;

				this.selectedElement = value;
				RaisePropertyChanged ();
			}
		}

		public ICommand CreateElementCommand
		{
			get;
		}

		protected async Task LoadAsync ()
		{
			this.elements.Reset (await LoadElementsAsync ());
		}

		protected override async void OnCampaignChanged ()
		{
			base.OnCampaignChanged ();
			await LoadAsync ();
		}

		protected virtual async Task<IReadOnlyList<TViewModel>> LoadElementsAsync ()
		{
			var elements = await GetCampaignElementsAsync<T> ();
			return elements.Select (e => CreateElementViewModel (e)).ToList ();
		}

		private readonly ObservableCollectionEx<TViewModel> elements = new ObservableCollectionEx<TViewModel> ();
		private TViewModel selectedElement;

		protected abstract TViewModel CreateElementViewModel (T element);

		protected abstract T InitializeElement (string name);

		protected virtual async Task<TViewModel> CreateElementAsync (string name)
		{
			T element = InitializeElement (name);
			element = await SyncService.SaveElementAsync (element);
			TViewModel vm = CreateElementViewModel (element);
			this.elements.Add (vm);
			return vm;
		}

		private async void OnElementsChanged(ElementsChangedMessage msg)
		{
			if (!typeof (T).IsAssignableFrom (msg.Type))
				return;

			await LoadAsync ();
		}
	}

	internal abstract class CampaignViewModel
		: DataViewModel
	{
		protected CampaignViewModel (IAsyncServiceProvider serviceProvider)
			: base (serviceProvider)
		{
			this.synchronizationContext = SynchronizationContext.Current;
		}

		protected CampaignManager CampaignManager
		{
			get;
			private set;
		}

		protected override async Task SetupAsync()
		{
			await base.SetupAsync ();

			CampaignManager = await ServiceProvider.GetServiceAsync<CampaignManager> ();
			MessengerInstance.Register<SingleSelectionChangedMessage> (this, s => {
				if (s.Type != typeof (CampaignElement))
					return;

				this.synchronizationContext.Post (s => {
					OnCampaignChanged ();
				}, null);
			});
		}

		protected async Task<IReadOnlyList<T>> GetCampaignElementsAsync<T> (bool includeNonCampaign = true)
			where T : CampaignChildElement
		{
			await SetupTask;
			var elements = await SyncService.GetElementsAsync<T> ();
			return elements.Where (e => (e.CampaignId == null && includeNonCampaign) || e.CampaignId == CampaignManager.SelectedElement.Id).ToList ();
		}

		protected virtual void OnCampaignChanged()
		{
		}

		private readonly SynchronizationContext synchronizationContext;
	}
}
