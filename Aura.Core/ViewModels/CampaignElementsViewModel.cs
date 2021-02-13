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
		: ElementsViewModel<T, TViewModel>
		where T : CampaignChildElement
		where TViewModel : ElementViewModel<T>
	{
		protected CampaignElementsViewModel (IAsyncServiceProvider services)
			: base (services)
		{
			this.synchronizationContext = SynchronizationContext.Current;
			MessengerInstance.Register<ElementsChangedMessage> (this, OnElementsChanged);

			OnCampaignChanged ();
			CreateElementCommand = new RelayCommand<string> (async s => {
				TViewModel vm = await CreateElementAsync (s);
				SelectedElement = vm;
			}, s => !String.IsNullOrWhiteSpace (s));
		}

		public ICommand CreateElementCommand
		{
			get;
		}

		protected CampaignManager CampaignManager
		{
			get;
			private set;
		}

		protected override async Task<IReadOnlyList<TViewModel>> LoadElementsAsync ()
		{
			var elements = await GetCampaignElementsAsync ();
			return elements.Select (e => InitializeElementViewModel (e)).ToList ();
		}

		protected async Task<IReadOnlyList<T>> GetCampaignElementsAsync (bool includeNonCampaign = true)
		{
			await SetupTask;
			var elements = await SyncService.GetElementsAsync<T> ();
			return elements.Where (e => (e.CampaignId == null && includeNonCampaign) || e.CampaignId == CampaignManager.SelectedElement.Id).ToList ();
		}

		protected override async Task SetupAsync ()
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

		protected virtual void OnCampaignChanged ()
		{
			RequestReload ();
		}

		private readonly SynchronizationContext synchronizationContext;

		private void OnElementsChanged(ElementsChangedMessage msg)
		{
			if (!typeof (T).IsAssignableFrom (msg.Type))
				return;

			RequestReload ();
		}
	}
}
