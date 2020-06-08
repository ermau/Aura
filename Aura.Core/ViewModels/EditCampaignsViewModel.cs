using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Input;

using Aura.Messages;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Aura.ViewModels
{
	internal class EditCampaignsViewModel
		: ViewModelBase
	{
		public EditCampaignsViewModel (ISyncService syncService, CampaignManager manager)
		{
			this.syncService = syncService;
			this.manager = manager ?? throw new ArgumentNullException (nameof (manager));
			((INotifyCollectionChanged)this.manager.Elements).CollectionChanged += OnManagerElementsChanged;
			OnManagerElementsChanged (this.manager, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));

			DeleteCampaign = new RelayCommand<Campaign> (OnDelete, CanDelete);
			JoinCampaign = new RelayCommand (() => MessengerInstance.Send (new RequestJoinCampaignPromptMessage ()));
		}

		public IReadOnlyList<EditCampaignViewModel> Campaigns
		{
			get;
			private set;
		}

		public Campaign SelectedCampaign
		{
			get => this.selectedCampaign;
			set
			{
				if (this.selectedCampaign == value)
					return;

				this.selectedCampaign = value;
				RaisePropertyChanged ();
				((RelayCommand)DeleteCampaign).RaiseCanExecuteChanged ();
			}
		}

		public ICommand DeleteCampaign
		{
			get;
		}

		public ICommand JoinCampaign
		{
			get;
		}

		private readonly ISyncService syncService;
		private readonly CampaignManager manager;
		private Campaign selectedCampaign;

		private void OnManagerElementsChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			Campaigns = this.manager.Elements.Select (c => new EditCampaignViewModel (c)).ToArray ();
		}

		private async void OnDelete (Campaign campaign)
		{
			await this.syncService.DeleteElementAsync (campaign);
		}

		private bool CanDelete (Campaign campaign) => !(campaign is null);
	}

	internal class EditCampaignViewModel
		: ViewModelBase
	{
		public EditCampaignViewModel (Campaign campaign)
		{
			this.campaign = campaign ?? throw new ArgumentNullException (nameof (campaign));
		}

		public string Name => this.campaign.Name;

		public ICommand RenameCampaign
		{
			get;
		}

		private readonly ISyncService syncService;
		private readonly Campaign campaign;
	}
}
