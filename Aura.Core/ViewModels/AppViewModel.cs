using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using GalaSoft.MvvmLight;

using Aura.Data;

namespace Aura.ViewModels
{
	internal class AppViewModel
		: ViewModelBase
	{
		public AppViewModel (ISyncService syncService, CampaignManager campaigns, PlaySpaceManager playSpaces)
		{
			Campaigns = campaigns ?? throw new ArgumentNullException (nameof (campaigns));
			PlaySpaces = playSpaces ?? throw new ArgumentNullException (nameof (playSpaces));
			this.syncService = syncService ?? throw new ArgumentNullException (nameof (syncService));
		}

		public CampaignManager Campaigns
		{
			get;
		}

		public PlaySpaceManager PlaySpaces
		{
			get;
		}

		public IReadOnlyList<NamedElement> SearchResults
		{
			get => this.searchResults;
			private set
			{
				if (this.searchResults == value)
					return;

				this.searchResults = value;
				RaisePropertyChanged ();
			}
		}

		public string SearchQuery
		{
			get => this.searchQuery;
			set
			{
				if (this.searchQuery == value)
					return;

				this.searchQuery = value;
				RaisePropertyChanged ();

				Search ();
			}
		}

		private readonly ISyncService syncService;
		private string searchQuery;
		private IReadOnlyList<NamedElement> searchResults;

		private async void Search ()
		{
			SearchResults = null;
			if (String.IsNullOrWhiteSpace (SearchQuery))
				return;

			SearchResults = await this.syncService.FindElementsByNameAsync (SearchQuery);
			if (SearchResults.Count == 0)
				SearchResults = new[] { new NamedElement { Name = "No Results" } };
		}
	}
}
