using System;
using daemon_console;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Threading.Tasks;

namespace CallRecordsSubscription
{
    public class RenewSubscription
    {
        [FunctionName("RenewSubscription")]
        //public static async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        public static async Task Run([TimerTrigger("0 0 6 * * *")]TimerInfo myTimer, ILogger log)
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
                app = daemon_console.GlobalFunction.GetAppAsync(config);
                log.LogInformation("Success login.");

                string[] scopes = new string[] { $"{config.ApiUrl}.default" }; // Generates a scope -> "https://graph.microsoft.com/.default"

                // Call MS graph using the Graph SDK
                log.LogInformation("Running Function: GetCallRecordsSDK");
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

                GraphServiceClient graphServiceClient = daemon_console.GlobalFunction.GetAuthenticatedGraphClient(app, scopes);
                await graphServiceClient.Subscriptions[subscriptionId].Request().UpdateAsync(subscription);

                log.LogInformation("Successfully update the subscription!");
            }
            catch (ServiceException e)
            {
                log.LogError(e.Error.Message);
            }
        }

    }
}
