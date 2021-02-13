using System.IO;
using System.Threading.Tasks;

namespace Aura
{
	public interface ILocalStorageService
	{
		Task DeleteAsync (string id, string contentHash = null);

		Task<Stream> TryGetStream (string id, string contentHash = null);

		Task<Stream> GetWriteStreamAsync (string id, string contentHash = null);
	}
}
