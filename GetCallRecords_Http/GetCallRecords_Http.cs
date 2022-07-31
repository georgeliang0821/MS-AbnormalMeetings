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
using Microsoft.Identity.Web;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using daemon_console;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using System.Text;
using System.Net;

namespace GetCallRecords_Http
{

    public class GetCallRecords_Http
    {
        [FunctionName("GetCallRecords_Http")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetCallRecords_Http is triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //log.LogInformation(requestBody);

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

                app = GetAppAsync(config);
                log.LogInformation("Success login.");

                log.LogInformation("Getting call records...");
                // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
                // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
                // a tenant administrator. 
                string[] scopes = new string[] { $"{config.ApiUrl}.default" }; // Generates a scope -> "https://graph.microsoft.com/.default"

                // Call MS graph using the Graph SDK
                log.LogInformation("Running Function: CallMSGraphUsingGraphSDK");
                CallRecord callrecord = await CallMSGraphUsingGraphSDK(app, scopes, meetingID, log);

                //log.LogInformation("Start time: " + callrecord.StartDateTime);
                //log.LogInformation("End time: " + callrecord.EndDateTime);
                string jsonString = System.Text.Json.JsonSerializer.Serialize(callrecord);
                log.LogInformation("jsonString: " + jsonString);

                log.LogInformation("Writing file...");
                await SaveToBlob(callrecord, log);
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

            return new BadRequestObjectResult("Something wrong happened!");

        }

        private static async Task SaveToBlob(CallRecord callrecord, ILogger log)
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(callrecord);

            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
            string containerName = Environment.GetEnvironmentVariable("BlobContainerName");
            BlobContainerClient container = new BlobContainerClient(connectionString, containerName);

            var blob = container.GetBlobClient(callrecord.Id + ".json");
            using (MemoryStream mem = new MemoryStream())
            {
                // Write to stream
                Byte[] info = new UTF8Encoding(true).GetBytes(jsonString);
                mem.Write(info, 0, info.Length);

                // Go back to beginning of stream
                mem.Position = 0;

                await blob.UploadAsync(mem, overwrite:true);

                // TODO: Handle the BlobAlreadyExists code
                //try
                //{
                //    await blob.UploadAsync(mem);
                //    //await blob.UploadAsync(mem, overwrite:true);
                //}
                //catch (WebException ex)
                //{
                //    Console.WriteLine("Enter WebException");
                //    if (ex.Status == WebExceptionStatus.ProtocolError)
                //    {
                //        var response = ex.Response as HttpWebResponse;
                //        // Handling response.StatusCode == 409 here! 
                //        if (response != null && (int)response.StatusCode == 409)
                //        {
                //            // If file exist: just overwrite it
                //            log.LogInformation("Try to catch error 409; ErrorCode: BlobAlreadyExists");
                //            await blob.UploadAsync(mem, overwrite: true);
                //            log.LogInformation("Success! Catch BlobAlreadyExists using overwrite");
                //        }
                //        else
                //        {
                //            // no http status code available
                //            log.LogError(ex.Message);
                //        }
                //    }
                //    else
                //    {
                //        // no http status code available
                //        log.LogError(ex.Message);
                //    }
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine("Enter Exception");
                //    try
                //    {
                //        Console.WriteLine("Enter Exception Catch");
                //        log.LogInformation("Enter Exception: " + ex.Message);
                //        await blob.UploadAsync(mem, overwrite:true);
                //        log.LogInformation("Success! Overcome the exception using overwrite.");
                //    }
                //    catch
                //    {
                //        Console.WriteLine("Enter Exception Catch");
                //        log.LogError(ex.Message);
                //    }
                //}
            }

        }

        private static IConfidentialClientApplication GetAppAsync(AuthenticationConfig config)
        {
            //// You can run this sample using ClientSecret or Certificate. The code will differ only when instantiating the IConfidentialClientApplication
            //bool isUsingClientSecret = IsAppUsingClientSecret(config);

            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app;

            // Even if this is a console application here, a daemon application is a confidential client application
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                .WithClientSecret(config.ClientSecret)
                .WithAuthority(new Uri(config.Authority))
                .Build();

            app.AddInMemoryTokenCache();

            return app;
        }

        /// <summary>
        /// Calls MS Graph REST API using an authenticated Http client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="app"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        private static async Task CallMSGraph(AuthenticationConfig config, IConfidentialClientApplication app, string[] scopes, string meetingID)
        {
            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token acquired");
                Console.ResetColor();
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }

            // The following example uses a Raw Http call 
            if (result != null)
            {
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                //await apiCaller.CallWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/users", result.AccessToken, Display);
                Console.WriteLine("Running: await apiCaller.CallWebApiAndProcessResultASync");
                await apiCaller.CallWebApiAndProcessResultASync(
                    $"{config.ApiUrl}v1.0/communications/callRecords/{meetingID}",
                    result.AccessToken, Display);
                    //$"{config.ApiUrl}v1.0/communications/callRecords/{meetingID}?$expand=sessions($expand=segments)",
            }
        }

        /// <summary>
        /// The following example shows how to initialize the MS Graph SDK
        /// </summary>
        /// <param name="app"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        private static async Task<CallRecord> CallMSGraphUsingGraphSDK(IConfidentialClientApplication app, string[] scopes, string meetingID, ILogger log)
        {
            // Prepare an authenticated MS Graph SDK client
            GraphServiceClient graphServiceClient = GetAuthenticatedGraphClient(app, scopes);

            try
            {
                CallRecord callrecord = await graphServiceClient.Communications.CallRecords[meetingID]
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

        /// <summary>
        /// An example of how to authenticate the Microsoft Graph SDK using the MSAL library
        /// </summary>
        /// <returns></returns>
        private static GraphServiceClient GetAuthenticatedGraphClient(IConfidentialClientApplication app, string[] scopes)
        {

            GraphServiceClient graphServiceClient =
                    new GraphServiceClient("https://graph.microsoft.com/V1.0/", new DelegateAuthenticationProvider(async (requestMessage) =>
                    {
                        // Retrieve an access token for Microsoft Graph (gets a fresh token if needed).
                        AuthenticationResult result = await app.AcquireTokenForClient(scopes)
                            .ExecuteAsync();

                        // Add the access token in the Authorization header of the API request.
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    }));

            return graphServiceClient;
        }


        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to display</param>
        private static void Display(JsonNode result)
        {
            if (result == null)
            {
                Console.WriteLine("The result content is null");
            }
            else
            {
                JsonArray nodes = ((result as JsonObject).ToArray()[1]).Value as JsonArray;

                foreach (JsonObject aNode in nodes.ToArray())
                {
                    foreach (var property in aNode.ToArray())
                    {
                        Console.WriteLine($"{property.Key} = {property.Value?.ToString()}");
                    }
                    Console.WriteLine();
                }
            }
        }

        ///// <summary>
        ///// Checks if the sample is configured for using ClientSecret or Certificate. This method is just for the sake of this sample.
        ///// You won't need this verification in your production application since you will be authenticating in AAD using one mechanism only.
        ///// </summary>
        ///// <param name="config">Configuration from appsettings.json</param>
        ///// <returns></returns>
        //private static bool IsAppUsingClientSecret(AuthenticationConfig config)
        //{
        //    string clientSecretPlaceholderValue = "[Enter here a client secret for your application]";

        //    if (!String.IsNullOrWhiteSpace(config.ClientSecret) && config.ClientSecret != clientSecretPlaceholderValue)
        //    {
        //        return true;
        //    }

        //    else if (config.Certificate != null)
        //    {
        //        return false;
        //    }

        //    else
        //        throw new Exception("You must choose between using client secret or certificate. Please update appsettings.json file.");
        //}

        public class SubscriptionData
        {
            public List<Value> value { get; set; }
        }

        public class Value
        {
            public string tenantId { get; set; }
            public string subscriptionId { get; set; }
            public string clientState { get; set; }
            public string changeType { get; set; }
            public string resource { get; set; }
            public DateTime subscriptionExpirationDateTime { get; set; }
            public ResourceData resourceData { get; set; }
        }

        public class ResourceData
        {
            public string oDataType { get; set; }
            public string oDataId { get; set; }
            public string id { get; set; }
        }

    }
}

