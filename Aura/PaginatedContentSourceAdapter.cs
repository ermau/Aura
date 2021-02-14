using System;
using System.Collections;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Aura.ViewModels;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Aura
{
	internal class PaginatedContentSourceConverter
		: IValueConverter
	{
		public object Convert (object value, Type targetType, object parameter, string language)
		{
			if (value is PaginatedContentSource source)
				return new PaginatedContentSourceAdapter (source);

			return null;
		}

		public object ConvertBack (object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException ();
		}
	}

	internal class PaginatedContentSourceAdapter
		: Windows.UI.Xaml.Data.ISupportIncrementalLoading, IList, INotifyCollectionChanged
	{
		private readonly PaginatedContentSource source;

		public PaginatedContentSourceAdapter (PaginatedContentSource source)
		{
			this.source = source ?? throw new ArgumentNullException (nameof (source));
		}

		public object this[int index] { get => ((IList)this.source)[index]; set => ((IList)this.source)[index] = value; }

		public bool IsFixedSize => ((IList)this.source).IsFixedSize;

		public bool IsReadOnly => ((IList)this.source).IsReadOnly;

		public int Count => ((ICollection)this.source).Count;

		public bool IsSynchronized => ((ICollection)this.source).IsSynchronized;

		public object SyncRoot => ((ICollection)this.source).SyncRoot;

		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add
			{
				((INotifyCollectionChanged)this.source).CollectionChanged += value;
			}

			remove
			{
				((INotifyCollectionChanged)this.source).CollectionChanged -= value;
			}
		}

		public int Add (object value)
		{
			return ((IList)this.source).Add (value);
		}

		public void Clear ()
		{
			((IList)this.source).Clear ();
		}

		public bool Contains (object value)
		{
			return ((IList)this.source).Contains (value);
		}

		public void CopyTo (Array array, int index)
		{
			((ICollection)this.source).CopyTo (array, index);
		}

		public IEnumerator GetEnumerator ()
		{
			return ((IEnumerable)this.source).GetEnumerator ();
		}

		public int IndexOf (object value)
		{
			return ((IList)this.source).IndexOf (value);
		}

		public void Insert (int index, object value)
		{
			((IList)this.source).Insert (index, value);
		}

		public void Remove (object value)
		{
			((IList)this.source).Remove (value);
		}

		public void RemoveAt (int index)
		{
			((IList)this.source).RemoveAt (index);
		}

		public IAsyncOperation<Windows.UI.Xaml.Data.LoadMoreItemsResult> LoadMoreItemsAsync (uint count)
		{
			return LoadMoreItemsAdapt (count).AsAsyncOperation ();
		}

		public bool HasMoreItems => this.source.HasMoreItems;

		private async Task<Windows.UI.Xaml.Data.LoadMoreItemsResult> LoadMoreItemsAdapt(uint count)
		{
			var result = await this.source.LoadMoreItemsAsync ((int)count);
			return new Windows.UI.Xaml.Data.LoadMoreItemsResult {
				Count = (uint)result.Count
			};
		}
	}
}
