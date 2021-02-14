using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using GalaSoft.MvvmLight;

using Aura.Data;
using System.Threading;
using System.Linq;
using System.ComponentModel;

namespace Aura.ViewModels
{
	internal class AppViewModel
		: ViewModelBase
	{
		public AppViewModel (ISyncService syncService, DownloadManager downloads, CampaignManager campaigns, PlaySpaceManager playSpaces)
		{
			Campaigns = campaigns ?? throw new ArgumentNullException (nameof (campaigns));
			PlaySpaces = playSpaces ?? throw new ArgumentNullException (nameof (playSpaces));
			this.syncService = syncService ?? throw new ArgumentNullException (nameof (syncService));

			this.synchronization = SynchronizationContext.Current;
			this.downloads = downloads ?? throw new ArgumentNullException (nameof (downloads));
			this.downloads.DownloadsChanged += OnDownloadsChanged;
		}

		public CampaignManager Campaigns
		{
			get;
		}

		public PlaySpaceManager PlaySpaces
		{
			get;
		}

		public IReadOnlyList<SearchResultItem> SearchResults => this.searchResults;

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

		public bool IsDownloading
		{
			get => this.isDownloading;
			set
			{
				if (this.isDownloading == value)
					return;

				this.isDownloading = value;
				RaisePropertyChanged ();
			}
		}

		public string DownloadText
		{
			get => this.downloadText;
			set
			{
				if (this.downloadText == value)
					return;

				this.downloadText = value;
				RaisePropertyChanged ();
			}
		}

		public double DownloadProgress
		{
			get => this.watchingDownload?.Progress ?? 0;
		}

		private readonly ISyncService syncService;
		private readonly DownloadManager downloads;
		private readonly SynchronizationContext synchronization;
		private string searchQuery;
		private ObservableCollectionEx<SearchResultItem> searchResults = new ObservableCollectionEx<SearchResultItem>();

		private bool isDownloading;
		private string downloadText = "Downloads"; // TODO: Localize
		private ManagedDownload watchingDownload;

		private async void Search ()
		{
			if (String.IsNullOrWhiteSpace (SearchQuery)) {
				this.searchResults = null;
				RaisePropertyChanged (nameof (SearchResults));
				return;
			} else if (this.searchResults == null) {
				this.searchResults = new ObservableCollectionEx<SearchResultItem> ();
			}

			IEnumerable<SearchResultItem> results;
			var rs = (await this.syncService.FindElementsByNameAsync (SearchQuery)).Select (ne => new SearchResultItem (ne)).Take (5).ToList();
			if (rs.Count == 0)
				results = new[] { new SearchResultItem ("No Results") };
			else
				results = rs;

			this.searchResults.Reset (results);
		}

		private void OnDownloadsChanged (object sender, EventArgs e)
		{
			this.synchronization.Post (s => {
				ManagedDownload watching = Interlocked.Exchange (ref this.watchingDownload, null);
				if (watching != null) {
					watching.PropertyChanged -= OnWatchingDownloadPropertyChanged;
				}

				IReadOnlyList<ManagedDownload> current = this.downloads.Downloads;
				if (current.Count == 0) {
					IsDownloading = false;
					DownloadText = "Downloads";
					return;
				}

				ManagedDownload last = current.Where (d => d.State == DownloadState.InProgress).LastOrDefault ();
				if (last == null) {
					IsDownloading = false;
					last = current.Last ();
				} else {
					DownloadText = last.Name;
					IsDownloading = true;
					this.watchingDownload = last;
					this.watchingDownload.PropertyChanged += OnWatchingDownloadPropertyChanged;
				}

				string prefix = String.Empty;
				if (current.Count > 1) {
					prefix = $"({current.Count (d => d.State != DownloadState.InProgress)} / {current.Count}) ";
				}

				DownloadText = prefix + last.Name;
				RaisePropertyChanged (nameof (DownloadProgress));
			}, null);			
		}

		private void OnWatchingDownloadPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (ManagedDownload.State))
				OnDownloadsChanged (this, EventArgs.Empty);
			else if (e.PropertyName == nameof(ManagedDownload.Progress))
				RaisePropertyChanged (nameof (DownloadProgress));
		}
	}

	internal class SearchResultItem
	{
		public SearchResultItem (NamedElement element)
		{
			Element = element ?? throw new ArgumentNullException (nameof (element));
			DisplayText = Element.Name;
			ActionText = "Go to details";
		}

		public SearchResultItem (string text)
		{
			if (string.IsNullOrWhiteSpace (text))
				throw new ArgumentException ($"'{nameof (text)}' cannot be null or whitespace", nameof (text));

			DisplayText = text;
		}

		public string DisplayText
		{
			get;
		}

		public NamedElement Element
		{
			get;
		}

		public string ActionText
		{
			get;
		}
	}
}
