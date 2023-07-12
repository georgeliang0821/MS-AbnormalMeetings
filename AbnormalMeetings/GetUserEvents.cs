using System;
using System.IO;
using System.Threading.Tasks;
using daemon_console;
using global_class;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace AbnormalMeetings
{
    public class GetUserEvents
    {
        private static ILogger mylog = null;

        [FunctionName("GetUserEvents")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            mylog = log;

            log.LogInformation("GetUserEvents is triggered.");

            daemon_console.GlobalFunction.PrintHeaders(req.Headers, log);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (requestBody == "")
            {
                log.LogInformation("The requestBody is NULL");
            }
            else
            {
                log.LogInformation("The requestBody is: " + requestBody);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////// This is a validation process, so rutrun and stop program here! ////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            string query_validationToken = req.Query["validationToken"];
            if (query_validationToken != null)
            {
                log.LogInformation("The validationToken is: " + query_validationToken);
                return new OkObjectResult(query_validationToken);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            SubscriptionData subscriptionData = JsonConvert.DeserializeObject<SubscriptionData>(requestBody);
            string resource = subscriptionData.value[0].resource;
            string filename = subscriptionData.value[0].resourceData.id + ".json";

            IConfidentialClientApplication app;
            try
            {
                log.LogInformation("Login to application...");

                AuthenticationConfig config = new AuthenticationConfig();

                // config.Instance = Environment.GetEnvironmentVariable("Instance");
                // config.ApiUrl = Environment.GetEnvironmentVariable("ApiUrl");
                config.Tenant = Environment.GetEnvironmentVariable("Tenant");
                config.ClientId = Environment.GetEnvironmentVariable("ClientId");
                config.ClientSecret = Environment.GetEnvironmentVariable("ClientSecret");

                string webApiUrl = $"{config.ApiUrl}v1.0/{resource}";

                app = daemon_console.GlobalFunction.GetAppAsync(config);
                log.LogInformation("Success login.");

                string[] scopes = new string[] { $"{config.ApiUrl}.default" }; // Generates a scope -> "https://graph.microsoft.com/.default"

                // Call MS graph using the Graph SDK
                log.LogInformation("Running Function: SaveUserEvents");
                string userEvent_Json = await daemon_console.GlobalFunction.GetHttpRequest(app, scopes, webApiUrl, log);

                string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
                string containerName = config.BlobContainerName_UserEvents;
                await daemon_console.GlobalFunction.SaveToBlob(filename, userEvent_Json, connectionString, containerName, log);

                return new OkObjectResult(JsonConvert.SerializeObject("Successfully run!"));

            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }

            return new BadRequestObjectResult("Something wrong happened! Please check your log!");
        }
    }
}
