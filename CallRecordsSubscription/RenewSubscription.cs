using System;
using daemon_console;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

using daemon_console;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CallRecordsSubscription
{
    public class RenewSubscription
    {
        //public void Run([TimerTrigger("0 17 11  * * *")]TimerInfo myTimer, ILogger log)
        [FunctionName("RenewSubscription")]
        public static async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            IConfidentialClientApplication app;
            try
            {
                AuthenticationConfig config = new AuthenticationConfig();

                config.Instance = Environment.GetEnvironmentVariable("Instance");
                config.ApiUrl = Environment.GetEnvironmentVariable("ApiUrl");
                config.Tenant = Environment.GetEnvironmentVariable("Tenant");
                config.ClientId = Environment.GetEnvironmentVariable("ClientId");
                config.ClientSecret = Environment.GetEnvironmentVariable("ClientSecret");

                // Build the app
                log.LogInformation("Login to application...");
                app = GetAppAsync(config);
                log.LogInformation("Success login.");

                string[] scopes = new string[] { $"{config.ApiUrl}.default" }; // Generates a scope -> "https://graph.microsoft.com/.default"

                // Call MS graph using the Graph SDK
                log.LogInformation("Running Function: CallMSGraphUsingGraphSDK");
                await CallMSGraphUsingGraphSDKAsync(config, app, scopes, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static async Task CallMSGraphUsingGraphSDKAsync(AuthenticationConfig config, IConfidentialClientApplication app, string[] scopes, ILogger log)
        {
            try
            {
                Double timeShift = 1440 * 2;
                //String expirationDateTime = "'" + DateTime.UtcNow.AddMinutes(timeShift).ToString("s") + "'";
                DateTime expirationDateTime = DateTime.UtcNow.AddMinutes(timeShift);

                var subscription = new Subscription
                {
                    ExpirationDateTime = expirationDateTime,
                };

                string subscriptionId = Environment.GetEnvironmentVariable("subscriptionId");
                log.LogInformation(String.Format("Try to renew: subscriptionId- {0}; ExpirationDateTime- {1}: ", subscriptionId, expirationDateTime));

                GraphServiceClient graphServiceClient = GetAuthenticatedGraphClient(app, scopes);
                await graphServiceClient.Subscriptions[subscriptionId].Request().UpdateAsync(subscription);

                log.LogInformation("Successfully update the subscription!");
            }
            catch (ServiceException e)
            {
                log.LogError(e.Error.Message);
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
        /// Build an app after authentication
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static IConfidentialClientApplication GetAppAsync(AuthenticationConfig config)
        {
            IConfidentialClientApplication app;

            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                .WithClientSecret(config.ClientSecret)
                .WithAuthority(new Uri(config.Authority))
                .Build();

            app.AddInMemoryTokenCache();

            return app;
        }


    }
}
