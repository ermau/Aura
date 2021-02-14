using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.ViewModels
{
	internal class PaginatedContentSource
		: ISupportIncrementalLoading, IList, INotifyCollectionChanged
	{
		public PaginatedContentSource (Func<ContentEntry, object> getViewModel, IContentProviderService service, ContentSearchOptions options, ContentPage initialPage)
		{
			if (initialPage is null)
				throw new ArgumentNullException (nameof (initialPage));

			this.getViewModel = getViewModel;
			this.service = service ?? throw new ArgumentNullException (nameof (service));
			this.options = options ?? throw new ArgumentNullException (nameof (options));

			this.items.AddRange (initialPage.Entries.Select (e => getViewModel (e)));
			this.pageSize = initialPage.Entries.Count;
		}

		public event Action<Task> LoadingRequested;

		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add { this.items.CollectionChanged += value; }
			remove { this.items.CollectionChanged -= value; }
		}

		public Task<LoadMoreItemsResult> LoadMoreItemsAsync (int count)
		{
			Task<LoadMoreItemsResult> resultTask = LoadCoreAsync (count);
			LoadingRequested?.Invoke (resultTask);
			return resultTask;
		}

		public bool HasMoreItems
		{
			get;
			private set;
		} = true;

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => true;

		int ICollection.Count => this.items.Count;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this.items;

		object IList.this[int index]
		{
			get => this.items[index];
			set => throw new NotSupportedException ();
		}

		private readonly ObservableCollectionEx<object> items = new ObservableCollectionEx<object> ();
		private readonly Func<ContentEntry, object> getViewModel;
		private readonly IContentProviderService service;
		private readonly ContentSearchOptions options;
		private readonly int pageSize;
		private int page = 1;

		private async Task<LoadMoreItemsResult> LoadCoreAsync(int count)
		{
			// TODO: Need some levers for determing if the service supports a page size
			// and if not how big theirs is.
			ContentPage page = await this.service.SearchAsync (new ContentSearchOptions {
				Query = this.options.Query,
				Page = ++this.page
			}, CancellationToken.None);

			this.items.AddRange (page.Entries.Select (e => getViewModel (e)));

			if (page.Entries.Count < this.pageSize)
				HasMoreItems = false;

			return new LoadMoreItemsResult {
				Count = page.Entries.Count
			};
		}

		int IList.Add (object value)
		{
			throw new NotSupportedException ();
		}

		void IList.Clear ()
		{
			throw new NotSupportedException ();
		}

		bool IList.Contains (object value)
		{
			throw new NotSupportedException ();
		}

		int IList.IndexOf (object value)
		{
			throw new NotSupportedException ();
		}

		void IList.Insert (int index, object value)
		{
			throw new NotSupportedException ();
		}

		void IList.Remove (object value)
		{
			throw new NotSupportedException ();
		}

		void IList.RemoveAt (int index)
		{
			throw new NotSupportedException ();
		}

		void ICollection.CopyTo (Array array, int index)
		{
			throw new NotSupportedException ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable<ContentEntry>)this).GetEnumerator ();
		}
	}
}
