using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aura
{
	interface ISupportIncrementalLoading
	{
		event Action<Task> LoadingRequested;

		Task<LoadMoreItemsResult> LoadMoreItemsAsync (int count);

		bool HasMoreItems { get; }
	}

	internal record LoadMoreItemsResult
	{
		public int Count { get; init; }
	}
}
