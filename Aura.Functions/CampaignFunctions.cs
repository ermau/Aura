using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Aura.Service;

namespace Aura.Functions
{

    public static class CampaignFunctions
    {
        [FunctionName ("createcampaign")]
        public static async Task<IActionResult> CreateCampaign (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", "post", Route = "campaigns/create")] HttpRequest req,
            [CosmosDB (databaseName: "campaigns", collectionName: "campaigns", ConnectionStringSetting = "CosmosDBConnection", CreateIfNotExists = true)] IAsyncCollector<RemoteCampaign> createdCampaigns,
            ILogger logger)
        {
            string name = req.Query["name"];
            if (name == null)
                return new BadRequestObjectResult ("Must include a name for the campaign");

            var id = Guid.NewGuid ();
            var document = new RemoteCampaign {
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
            [CosmosDB ("campaigns", "campaigns", ConnectionStringSetting = "CosmosDBConnection", SqlQuery = "select c.id,c.Name from campaigns c where c.id = {id} offset 0 limit 1")] IEnumerable<RemoteCampaign> campaigns,
            ILogger logger)
        {
            var campaign = campaigns.FirstOrDefault ();
            return (campaign != null) ? (ActionResult)new OkObjectResult (campaign) : new NotFoundObjectResult (null);
        }
    }
}
