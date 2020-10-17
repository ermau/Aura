using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Service.Client
{
	public interface ILiveCampaignClient
	{
		Task<RemoteCampaign> CreateCampaignAsync (string name, CancellationToken cancelToken);
		Task<RemoteCampaign> GetCampaignDetailsAsync (string id, CancellationToken cancelToken);
		Task<RemoteCampaign> GetCampaignDetailsAsync (Uri uri, CancellationToken cancelToken);

		Task<ICampaignConnection> ConnectToCampaignAsync (string id, IAsyncServiceProvider serviceProvider, CancellationToken cancelToken);
	}
}
