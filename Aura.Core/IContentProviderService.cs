using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Data;

namespace Aura
{
	public interface IContentProviderService
		: IService
	{
		Task<ContentPage> SearchAsync (ContentSearchOptions options, CancellationToken cancellationToken);

		Task<ContentEntry> GetEntryAsync (string id, CancellationToken cancellationToken);
		Task<Stream> DownloadEntryAsync (string id);
	}

	public record ContentPage
	{
		public int Page { get; init; }

		public IReadOnlyList<ContentEntry> Entries { get; init; }
	}

	public record ContentEntry
	{
		public string Id { get; init; }

		public string Name { get; init; }
		
		public string SourceUrl { get; init; }

		public string Description { get; init; }

		public ContentAuthor Author { get; init; }

		public string License { get; init; }

		public long Size { get; init; }

		public TimeSpan Duration { get; init; }

		public IReadOnlyList<ContentEntryPreview> Previews;
	}

	public record ContentEntryPreview
	{
		public string Url;
	}

	public record ContentSearchOptions
	{
		public int Page
		{
			get;
			init;
		}

		public string Query
		{
			get;
			init;
		}
	}
}
