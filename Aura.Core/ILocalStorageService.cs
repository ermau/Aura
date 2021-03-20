using System;
using System.IO;
using System.Threading.Tasks;

namespace Aura
{
	public interface ILocalStorageService
	{
		Task DeleteAsync (string id, string contentHash = null);

		/// <remarks>
		/// While the user could technically delete the file inbetween this call and use, the chances
		/// are fairly low so we'll just error out instead of trying to handle it.
		/// </remarks>
		Task<bool> GetIsPresentAsync (string id, string contentHash = null);
		Task<bool> GetIsPresentAsync (Uri fileUri);

		Task<Stream> TryGetStream (string id, string contentHash = null);
		Task<Stream> TryGetStream (Uri fileUri, string contentHash = null);

		Task<Stream> GetWriteStreamAsync (string id, string contentHash = null);
	}
}
