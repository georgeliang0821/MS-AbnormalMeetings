using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Microsoft.Graph;
using Microsoft.Graph.CallRecords;
using Microsoft.Identity.Client;
using daemon_console;
using global_class;

namespace AbnormalMeetings
{

    public class GetCallRecords
    {
        [FunctionName("GetCallRecords")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetCallRecords_Http is triggered.");

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
            string meetingID = subscriptionData.value[0].resourceData.id;

            IConfidentialClientApplication app;
            try
            {
                log.LogInformation("Login to application...");

                AuthenticationConfig config = new AuthenticationConfig();

                config.Instance = Environment.GetEnvironmentVariable("Instance");
                config.ApiUrl = Environment.GetEnvironmentVariable("ApiUrl");
                config.Tenant = Environment.GetEnvironmentVariable("Tenant");
                config.ClientId = Environment.GetEnvironmentVariable("ClientId");
                config.ClientSecret = Environment.GetEnvironmentVariable("ClientSecret");

                app = daemon_console.GlobalFunction.GetAppAsync(config);
                log.LogInformation("Success login.");

                log.LogInformation("Getting call records...");
                string[] scopes = new string[] { $"{config.ApiUrl}.default" }; // Generates a scope -> "https://graph.microsoft.com/.default"

                // Call MS graph using the Graph SDK
                log.LogInformation("Running Function: GetCallRecordsSDK");
                CallRecord callrecord = await GetCallRecordsSDK(app, scopes, meetingID, log);

                string filename = callrecord.Id + ".json";
                string jsonString = System.Text.Json.JsonSerializer.Serialize(callrecord);
                log.LogInformation("jsonString: " + jsonString);

                string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
                string containerName = Environment.GetEnvironmentVariable("BlobContainerName_CallRecords");

                log.LogInformation("Writing file...");
                await daemon_console.GlobalFunction.SaveToBlob(filename, jsonString, connectionString,containerName, log);
                log.LogInformation("Success writing file: " + meetingID + ".json");
                
                //// Call MS Graph REST API directly
                //log.LogInformation("Running Function: CallMSGraph");
                //await CallMSGraph(config, app, scopes, meetingID);

                return new OkObjectResult(JsonConvert.SerializeObject("Successfully run!"));

            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }

            return new BadRequestObjectResult("Something wrong happened! Please check your log!");

        }

        /// <summary>
        /// The following example shows how to initialize the MS Graph SDK
        /// </summary>
        /// <param name="app"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        private static async Task<CallRecord> GetCallRecordsSDK(IConfidentialClientApplication app, string[] scopes, string call_Id, ILogger log)
        {
            // Prepare an authenticated MS Graph SDK client
            GraphServiceClient graphServiceClient = daemon_console.GlobalFunction.GetAuthenticatedGraphClient(app, scopes);

            try
            {
                CallRecord callrecord = await graphServiceClient.Communications.CallRecords[call_Id]
                    .Request()
                    .Expand("sessions($expand=segments)")
                    .GetAsync();

                return callrecord;
            }
            catch (ServiceException e)
            {
                log.LogInformation("We could not retrieve the user's list: " + $"{e}");
            }
            return null;
        }



    }
}

