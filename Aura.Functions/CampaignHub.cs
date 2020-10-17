using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

using Aura.Service;
using Aura.Service.Messages;

namespace Aura.Functions
{
	public class CampaignHub
		: ServerlessHub
	{
		public const string OwnerClaim = "owner";

		[FunctionName("negotiate")]
		public SignalRConnectionInfo Negotiate (
			[HttpTrigger (AuthorizationLevel.Anonymous, Route = "CampaignHub/negotiate")] HttpRequest request,
			[SignalRConnectionInfo(HubName="CampaignHub")]SignalRConnectionInfo connectionInfo,
			ILogger logger)
		{
			logger.LogInformation ($"negotiate");
			return connectionInfo;
		}

		[FunctionName (nameof (OnConnected))]
		public async Task OnConnected (
			[SignalRTrigger] InvocationContext context,
			ILogger logger)
		{
			logger.LogInformation ($"{context.ConnectionId}:{context.UserId} connected");
		}

		[FunctionName (nameof (OnDisconnected))]
		public async Task OnDisconnected(
			[SignalRTrigger] InvocationContext context,
			ILogger logger)
		{
			logger.LogInformation ($"{context.ConnectionId}:{context.UserId} disconnected");
		}

		[FunctionName(nameof(StartGame))]
		public StartGameResult StartGame(
			[SignalRTrigger] InvocationContext context,
			[SignalRParameter] StartGameMessage message,
			[CosmosDB ("campaigns", "campaigns", ConnectionStringSetting = "CosmosDBConnection", Id = "{message.CampaignId}")] RemoteCampaign campaign,
			ILogger logger)
		{
			if (campaign == null)
				return StartGameResult.CampaignNotFound;

			logger.LogInformation ($"{context.ConnectionId}:{context.UserId} attempting to start game {message.CampaignId}");
			if (message.CampaignSecret != campaign?.Secret.ToString()) {
				return StartGameResult.Unauthorized;
			}

			logger.LogInformation ($"{context.ConnectionId}:{context.UserId} authorized to start game {message.CampaignId}");
			context.Claims.Add (OwnerClaim, "true");
			return StartGameResult.Success;
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple =true, Inherited =true)]
	internal class OwnerOnlyAttribute
		: SignalRFilterAttribute
	{
		public override Task FilterAsync (InvocationContext invocationContext, CancellationToken cancellationToken)
		{
			if (invocationContext.Claims.TryGetValue (CampaignHub.OwnerClaim, out string claim) && Boolean.TryParse (claim, out bool isOwner) && isOwner)
				return Task.CompletedTask;

			throw new UnauthorizedAccessException ();
		}
	}
}
