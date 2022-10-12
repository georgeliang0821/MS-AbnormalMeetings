using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using Azure.Core;
using Azure.Storage.Blobs;
using daemon_console;
using global_class;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.CallRecords;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.VisualBasic;
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
                string userEvent_Json = await SaveUserEvents(app, scopes, webApiUrl, log);

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




        /// <summary>
        /// The following example shows how to initialize the MS Graph SDK
        /// </summary>
        /// <param name="app"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        private static async Task<string> SaveUserEvents(IConfidentialClientApplication app, string[] scopes, string webApiUrl, ILogger log)
        {
            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                log.LogInformation("Token acquired");
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                log.LogInformation("Scope provided is not supported");
            }

            if (result != null)
            {
                string accessToken = result.AccessToken;
                var httpClient = new HttpClient();

                HttpRequestHeaders defaultRequestHeaders1 = httpClient.DefaultRequestHeaders;
                var defaultRequestHeaders = defaultRequestHeaders1;
                if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }
                defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await httpClient.GetAsync(webApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return json;
                }
                else
                {
                    log.LogInformation($"Failed to call the web API: {response.StatusCode}");
                    string content = await response.Content.ReadAsStringAsync();

                    // Note that if you got reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
                    // this is because the tenant admin as not granted consent for the application to call the Web API
                    log.LogInformation($"Content: {content}");
                }

                return null;
            }

            return null;
        }
    }
}
