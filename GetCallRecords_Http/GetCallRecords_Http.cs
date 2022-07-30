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
using System.Net;
using Newtonsoft.Json.Linq;

//using GraphWebhooks.Helpers;
//using GraphWebhooks.Models;
//using GraphWebhooks.SignalR;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using System.Web.Mvc;

namespace GetCallRecords_Http
{
    public static class GetCallRecords_Http
    {
        [FunctionName("GetCallRecords_Http")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Our GetCallRecords_Http is triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            log.LogInformation(requestBody);

            string query_validationToken = req.Query["validationToken"];
            if (query_validationToken != null)
            {
                log.LogInformation("The validationToken is: ");
                log.LogInformation(query_validationToken);

                return new OkObjectResult(query_validationToken);
            }

            //return new OkResult();

            string meetingID = "68dfa85b-3d18-44de-937e-a639d313c6ff";

            IConfidentialClientApplication app;
            try
            {
                log.LogInformation("Getting call records...");

                AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("loginsettings.json");
          
                app = GetAppAsync(config);

                // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
                // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
                // a tenant administrator. 
                string[] scopes = new string[] { $"{config.ApiUrl}.default" }; // Generates a scope -> "https://graph.microsoft.com/.default"

                // Call MS graph using the Graph SDK
                await CallMSGraphUsingGraphSDK(app, scopes, meetingID, log);

                // Call MS Graph REST API directly
                // await CallMSGraph(config, app, scopes);

                AuthenticationResult result = null;
                result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

                var response = new
                    {
                        AccessToken = result.AccessToken,
                        x = "hello",
                        y = "world",

                    };
                    //if (query_validationToken != null)
                    //{
                    //    return new OkObjectResult(query_validationToken);
                    //}

                return new OkObjectResult(JsonConvert.SerializeObject(response));

            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }

            return new BadRequestResult();

            //string responseMessage = "Token is token";
            //return new OkObjectResult(responseMessage);

            //if (req.QueryString["validationToken"] != null)
            //{
            //    var token = Request.QueryString["validationToken"];
            //    return Content(token, "text/plain");
            //}

        }


        private static IConfidentialClientApplication GetAppAsync(AuthenticationConfig config)
        {

            // You can run this sample using ClientSecret or Certificate. The code will differ only when instantiating the IConfidentialClientApplication
            bool isUsingClientSecret = IsAppUsingClientSecret(config);

            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app;

            if (isUsingClientSecret)
            {
                // Even if this is a console application here, a daemon application is a confidential client application
                app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
            }

            else
            {
                ICertificateLoader certificateLoader = new DefaultCertificateLoader();
                certificateLoader.LoadIfNeeded(config.Certificate);

                app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithCertificate(config.Certificate.Certificate)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
            }

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
        private static async Task CallMSGraph(AuthenticationConfig config, IConfidentialClientApplication app, string[] scopes)
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
                await apiCaller.CallWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/users", result.AccessToken, Display);

            }
        }

        /// <summary>
        /// The following example shows how to initialize the MS Graph SDK
        /// </summary>
        /// <param name="app"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        private static async Task CallMSGraphUsingGraphSDK(IConfidentialClientApplication app, string[] scopes, string meetingID, ILogger log)
        {
            // Prepare an authenticated MS Graph SDK client
            GraphServiceClient graphServiceClient = GetAuthenticatedGraphClient(app, scopes);


            // List<User> allUsers = new List<User>();

            try
            {

                // IGraphServiceUsersCollectionPage users = await graphServiceClient.Users.Request().GetAsync();
                // Console.WriteLine($"Found {users.Count()} users in the tenant"); 

                CallRecord callrecord = await graphServiceClient.Communications.CallRecords[meetingID]
                    .Request()
                    .GetAsync();

                log.LogInformation("Start time: " + callrecord.StartDateTime);
                log.LogInformation("End time: " + callrecord.EndDateTime);

            }
            catch (ServiceException e)
            {
                log.LogInformation("We could not retrieve the user's list: " + $"{e}");
            }

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

        /// <summary>
        /// Checks if the sample is configured for using ClientSecret or Certificate. This method is just for the sake of this sample.
        /// You won't need this verification in your production application since you will be authenticating in AAD using one mechanism only.
        /// </summary>
        /// <param name="config">Configuration from appsettings.json</param>
        /// <returns></returns>
        private static bool IsAppUsingClientSecret(AuthenticationConfig config)
        {
            string clientSecretPlaceholderValue = "[Enter here a client secret for your application]";

            if (!String.IsNullOrWhiteSpace(config.ClientSecret) && config.ClientSecret != clientSecretPlaceholderValue)
            {
                return true;
            }

            else if (config.Certificate != null)
            {
                return false;
            }

            else
                throw new Exception("You must choose between using client secret or certificate. Please update appsettings.json file.");
        }
    }
}

