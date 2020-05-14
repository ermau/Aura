using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

using Aura.Service;

namespace Aura.Functions
{

    public static class CampaignFunctions
    {
        [FunctionName ("createcampaign")]
        public static async Task<IActionResult> CreateCampaign (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", "post", Route = "campaigns/create")] HttpRequest req,
            [CosmosDB (databaseName: "campaigns", collectionName: "campaigns", ConnectionStringSetting = "CosmosDBConnection", CreateIfNotExists = true)] IAsyncCollector<Campaign> createdCampaigns,
            ILogger logger)
        {
            string name = req.Query["name"];
            if (name == null)
                return new BadRequestObjectResult ("Must include a name for the campaign");

            var id = Guid.NewGuid ();
            var document = new Campaign {
                id = id,
                Part = id.ToString()[0].ToString(),
                Name = name,
                Secret = Guid.NewGuid()
            };

            logger.LogInformation ($"Creating campaign {document.Name} at {document.id}");
            await createdCampaigns.AddAsync (document);

            return new OkObjectResult (document);
        }

        [FunctionName ("getcampaign")]
        public static IActionResult GetCampaign (
            [HttpTrigger (AuthorizationLevel.Anonymous, Route = "campaigns/{id}")] HttpRequest req,
            [CosmosDB ("campaigns", "campaigns", ConnectionStringSetting = "CosmosDBConnection", SqlQuery = "select c.id,c.Name from campaigns c where c.id = {id} offset 0 limit 1")] IEnumerable<Campaign> campaigns,
            ILogger logger)
        {
            var campaign = campaigns.FirstOrDefault ();
            return (campaign != null) ? (ActionResult)new OkObjectResult (campaign) : new NotFoundObjectResult (null);
        }

        [FunctionName ("negotiate")]
        public static async Task<SignalRConnectionInfo> Negotiate (
           [HttpTrigger (AuthorizationLevel.Anonymous, "post", Route = "campaigns/{id}/negotiate")] HttpRequest req,
           [CosmosDB ("campaigns", "campaigns", ConnectionStringSetting = "CosmosDBConnection", SqlQuery = "select 1 from campaigns c where c.id = {id} offset 0 limit 1")] IEnumerable<Campaign> campaigns,
           [SignalRConnectionInfo (HubName = "play", UserId = "{query.userid}")] SignalRConnectionInfo connection,
           ILogger logger)
        {
            Campaign campaign = campaigns.FirstOrDefault ();
            if (campaign == null)
                return null;

            return connection;
        }

        [FunctionName ("connect")]
        public static async Task<IActionResult> Connect (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "campaigns/{id}/connect")] HttpRequest req,
			string id,
            [SignalR (HubName = "play")] IAsyncCollector<SignalRGroupAction> groupActions,
            [CosmosDB ("campaigns", "campaigns", ConnectionStringSetting = "CosmosDBConnection", SqlQuery = "select 1 from campaigns c where c.id = {id} offset 0 limit 1")] IEnumerable<Campaign> campaigns,
            ILogger logger)
        {
            Campaign campaign = campaigns.FirstOrDefault ();
            if (campaign == null)
                return new NotFoundObjectResult (null);

            await groupActions.AddAsync (new SignalRGroupAction {
                Action = GroupAction.Add,
                GroupName = id,
                UserId = req.Query["userid"]
            });

            return new OkResult ();
        }

        [FunctionName ("prepareLayer")]
        public static Task PrepareLayer (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "campaigns/{id}/prepareLayer")] HttpRequest req,
            [CosmosDB ("campaigns", "campaigns", ConnectionStringSetting = "CosmosDBConnection", SqlQuery = "select c.Secret from campaigns c where c.id = {id} offset 0 limit 1")] IEnumerable<Campaign> campaigns,
            string id,
            [SignalR (HubName = "play")] IAsyncCollector<SignalRMessage> messages,
            ILogger logger)
        {
			Task auth = Authorize (req, campaigns);
			if (auth != Task.CompletedTask)
				return auth;

            return messages.AddAsync (new SignalRMessage {
                GroupName = id,
                Target = "prepareLayer",
                Arguments = new[] { "argument1" }
            });
        }

        private static Task Authorize (HttpRequest req, IEnumerable<Campaign> campaigns)
        {
            string secret = req.Query["secret"];
            if (secret == null)
                return Task.FromResult (new UnauthorizedResult ());

            Campaign c = campaigns.FirstOrDefault ();
            if (c == null)
                return Task.FromResult (new NotFoundResult ());
            if (c.Secret.ToString () != secret)
                return Task.FromResult (new UnauthorizedResult ());

            return Task.CompletedTask;
        }
    }
}
