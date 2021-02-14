using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
	class ContentSearchViewModel
		: BusyViewModel
	{
		public ContentSearchViewModel (IAsyncServiceProvider services)
		{
			this.services = services ?? throw new ArgumentNullException (nameof (services));
			Setup ();
		}

		public IContentProviderService SelectedService
		{
			get => this.selectedService;
			set
			{
				if (this.selectedService == value)
					return;

				this.selectedService = value;
				RaisePropertyChanged ();
				Search ();
			}
		}

		public IReadOnlyList<IContentProviderService> Services => this.contentServices;
		
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

		public IList Results
		{
			get => this.results;
		}

		public SearchResultItemViewModel SelectedSearchItem
		{
			get => this.selectedItem;
			set
			{
				if (this.selectedItem == value)
					return;

				this.selectedItem = value;
				RaisePropertyChanged ();
				value?.RequestDetails ();
			}
		}

		private readonly IAsyncServiceProvider services;
		private readonly ObservableCollectionEx<IContentProviderService> contentServices = new ObservableCollectionEx<IContentProviderService> ();
		private PaginatedContentSource results;
		private string searchQuery;
		private CancellationTokenSource cancel;
		private IContentProviderService selectedService;
		private SearchResultItemViewModel selectedItem;

		private async void Setup ()
		{
			AddWork ();

			var auth = await this.services.GetServiceAsync<IAuthenticationService> ();
			var services = await this.services.GetServicesAsync<IContentProviderService> ();

			var cs = new List<IContentProviderService> (services.Length);

			foreach (IContentProviderService service in services) {
				if (service is IAuthenticatedService authedService) {
					using (var cancel = new CancellationTokenSource (5000)) {
						try {
							await auth.TryAuthenticateAsync (authedService, cancel.Token);
							cs.Add (service);
						} catch {
						}
					}
				} else {
					cs.Add (service);
				}
			}

			this.contentServices.Reset (cs);
			RaisePropertyChanged (nameof (Services));

			SelectedService = cs.FirstOrDefault ();
			FinishWork ();
		}

		private async void Search ()
		{
			this.cancel?.Cancel ();

			if (this.results != null)
				this.results.LoadingRequested -= OnLoadingRequested;

			var service = SelectedService;
			if (service == null) {
				this.cancel = null;
				return;
			}

			string searchQuery = SearchQuery;
			if (String.IsNullOrWhiteSpace (searchQuery))
				return;

			AddWork ();

			var source = new CancellationTokenSource ();
			this.cancel = source;

			try {
				var search = new ContentSearchOptions {
					Query = searchQuery
				};
				ContentPage page = await SelectedService.SearchAsync (search, source.Token);

				if (source.Token.IsCancellationRequested)
					return;

				this.results = new PaginatedContentSource (ce => new SearchResultItemViewModel (ce, service, this.services), service, search, page);
				this.results.LoadingRequested += OnLoadingRequested;
				RaisePropertyChanged (nameof (Results));
			} catch (OperationCanceledException) {
			} catch (Exception) {
			} finally {
				FinishWork ();
			}
		}

		private async void OnLoadingRequested (Task loadingTask)
		{
			AddWork ();
			try {
				await loadingTask;
			} finally {
				FinishWork ();
			}
		}
	}

	internal class SearchResultItemViewModel
		: BusyViewModel
	{
		public SearchResultItemViewModel (ContentEntry entry, IContentProviderService service, IAsyncServiceProvider services)
		{
			Entry = entry;
			this.service = service ?? throw new ArgumentNullException (nameof (service));
			this.services = services ?? throw new ArgumentNullException (nameof (services));
			ImportCommand = new RelayCommand (OnImport);
			GoToCommand = new RelayCommand (() => MessengerInstance.Send (new NavigateToElementMessage (SampleId, typeof (FileSample))), () => SampleId != null);
			License = ContentLicense.GetLicense (entry.License);
		}

		public ContentEntry Entry
		{
			get => this.entry;
			private set
			{
				if (this.entry == value)
					return;

				this.entry = value;
				RaisePropertyChanged ();
			}
		}

		public Uri PreviewUri
		{
			get => this.previewUrl;
			set
			{
				if (this.previewUrl == value)
					return;

				this.previewUrl = value;
				RaisePropertyChanged ();
			}
		}

		public ContentLicense License
		{
			get;
		}

		public bool ShowImport => !IsDownloading && SampleId == null;
		public bool ShowGoto => !ShowImport && SampleId != null;

		public bool IsDownloading
		{
			get => this.download != null;
		}

		public double DownloadProgress
		{
			get => this.download?.Progress ?? 0;
		}

		public ICommand ImportCommand
		{
			get;
		}

		public ICommand GoToCommand
		{
			get;
		}

		public string SampleId => this.sample?.Id;

		public async void RequestDetails()
		{
			if (this.loadDetailsTask != null)
				return;

			this.loadDetailsTask = LoadDetailsAsync (CancellationToken.None);
			await this.loadDetailsTask;
		}

		public async Task LoadDetailsAsync (CancellationToken cancellation)
		{
			AddWork ();
			try {
				var entryTask = this.service.GetEntryAsync (this.entry.Id, cancellation);
				
				this.sync = await this.services.GetServiceAsync<ISyncService> ();
				this.sample = (await this.sync.FindElements<FileSample> (fs => fs.SourceUrl == this.entry.SourceUrl)).FirstOrDefault ();

				Entry = await entryTask;
				var preview = Entry.Previews.FirstOrDefault ();
				if (preview != null) {
					PreviewUri = new Uri (preview.Url);
				}

				UpdateSample ();
			} catch (OperationCanceledException) {
			} finally {
				FinishWork ();
			}
		}

		private readonly IContentProviderService service;
		private readonly IAsyncServiceProvider services;
		private ISyncService sync;
		private ContentEntry entry;
		private Uri previewUrl;
		private ManagedDownload download;
		private Task loadDetailsTask;
		private FileSample sample;

		private void UpdateSample()
		{
			RaisePropertyChanged (nameof (SampleId));
			RaisePropertyChanged (nameof (ShowImport));
			RaisePropertyChanged (nameof (ShowGoto));
			((RelayCommand)GoToCommand).RaiseCanExecuteChanged ();
		}

		private async void OnImport()
		{
			RequestDetails ();
			await this.loadDetailsTask;

			try {
				Guid newSampleId = Guid.NewGuid ();

				DownloadManager downloads = await this.services.GetServiceAsync<DownloadManager> ();
				this.download = downloads.QueueDownload (newSampleId.ToString(), Entry.Name, this.service.DownloadEntryAsync (entry.Id), Entry.Size);
				this.download.PropertyChanged += OnDownloadPropertyChanged;

				RaisePropertyChanged (nameof (ShowImport));
				RaisePropertyChanged (nameof (IsDownloading));

				string hash = await this.download.DownloadTask;

				var sample = new FileSample {
					Id = newSampleId.ToString(),
					Name = entry.Name,
					ContentHash = hash,
					Duration = entry.Duration,
					Author = entry.Author,
					SourceUrl = entry.SourceUrl,
					License = License
				};

				this.sample = await this.sync.SaveElementAsync (sample);
				UpdateSample ();
			} catch (Exception ex) {
				Trace.WriteLine ("Failed importing content: " + ex);
			} finally {
				if (this.download != null) {
					this.download.PropertyChanged -= OnDownloadPropertyChanged;
				}
			}
		}

		private void OnDownloadPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ManagedDownload.Progress)) {
				SynchronizationContext.Post (s => {
					RaisePropertyChanged (nameof (DownloadProgress));
				}, null);
			}
		}
	}
}
