using System;
using daemon_console;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using System.IO;
using Newtonsoft.Json;
using static GetCallRecords_Http.GetCallRecords_Http;
using static CallRecordsSubscription.RenewSubscription;
using global_class;

namespace CallRecordsSubscription
{
    public class RenewSubscription
    {
        [FunctionName("RenewSubscription")]
        //public static async Task Run([TimerTrigger("*/15 * * * * *")]TimerInfo myTimer, ILogger log)
        public static async Task Run([TimerTrigger("0 0 6 * * *")] TimerInfo myTimer, ILogger log)
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
            // SubscriptionList newSubscriptionList = new SubscriptionList();
            SubscriptionList subscriptionList;

            string webhook_UserEvent = Environment.GetEnvironmentVariable("Webhook_UserEvents");
            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
            string containerName = Environment.GetEnvironmentVariable("BlobContainerName_SubscriptionList");
            string filename = Environment.GetEnvironmentVariable("BlobFileName");
            
            // Read file from blob
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            BlobClient blobClient = containerClient.GetBlobClient(filename);

            log.LogInformation("Getting file in azure function");
            Dictionary<string, string> subscriptionDict = new Dictionary<string, string>();
            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadAsync();
                var responseStream = response.Value.Content;
                string responseString = await new StreamReader(responseStream).ReadToEndAsync();

                subscriptionList = JsonConvert.DeserializeObject<SubscriptionList>(responseString);
                foreach (var subscriptionInfo in subscriptionList.value)
                {
                    subscriptionDict.Add(subscriptionInfo.UserId, subscriptionInfo.SubscriptionId);
                    // log.LogInformation(subscriptionInfo.UserId + " : " + subscriptionDict[subscriptionInfo.UserId]);
                }
            }
            else
            {
                log.LogInformation("The target file do not exist in the container!");
                return;
            }

            // Start to create or renew the subscription
            IGraphServiceUsersCollectionPage users = null;
            try
            { 
                GraphServiceClient graphServiceClient = daemon_console.GlobalFunction.GetAuthenticatedGraphClient(app, scopes);
                users = await graphServiceClient.Users.Request().GetAsync();

                log.LogInformation("Found # of user: " + users.Count);

                //Double timeShift = 1440 * 3 - 10;
                Double timeShift = 1440 * 2;
                DateTime expirationDateTime = DateTime.UtcNow.AddMinutes(timeShift);

                foreach (var user in users)
                {
                    log.LogInformation("Current user ID: " + user.Id);
                    
                    // Renew
                    if (subscriptionDict.ContainsKey(user.Id))
                    {
                        var subscription = new Subscription
                        {
                            ExpirationDateTime = expirationDateTime,
                        };

                        //string subscriptionId = Environment.GetEnvironmentVariable("subscriptionId");
                        log.LogInformation(String.Format("\tTry to renew: subscriptionId- {0}; ExpirationDateTime- {1}: ", subscriptionDict[user.Id], expirationDateTime));

                        await graphServiceClient.Subscriptions[subscriptionDict[user.Id]].Request().UpdateAsync(subscription);
                        log.LogInformation("\tSuccessfully update the subscription!");
                    }
                    // Create
                    else
                    {
                        var subscription = new Subscription
                        {
                            ChangeType = "created,updated",
                            NotificationUrl = webhook_UserEvent,
                            Resource = "/users/" + user.Id + "/events",
                            ExpirationDateTime = expirationDateTime,
                            ClientState = "secretClientValue",
                            LatestSupportedTlsVersion = "v1_2"
                        };

                        try
                        {
                            Subscription responseSubscription = await graphServiceClient.Subscriptions
                            .Request()
                            .AddAsync(subscription);

                            SubscriptionInfo subscriptionInfo = new SubscriptionInfo();
                            subscriptionInfo.UserId = user.Id;
                            subscriptionInfo.SubscriptionId = responseSubscription.Id;
                            subscriptionList.value.Add(subscriptionInfo);

                            log.LogInformation("\tSuccessfully create the subscription");
                            log.LogInformation("\tUserId: " + user.Id);

                        }
                        catch
                        {
                            log.LogInformation("There is an error when processing events! (May be MailboxNotEnabledForRESTAPI)");
                            log.LogInformation("\tUserId: " + user.Id);
                        }

                        break;
                    }
                    // TODO: catch the exact problem

                }
                string jsonString = System.Text.Json.JsonSerializer.Serialize(subscriptionList);
                await daemon_console.GlobalFunction.SaveToBlob(filename, jsonString, connectionString, containerName, log);
                log.LogInformation("Successfully renew the subscriptionList.");

            }
            catch (ServiceException e)
            {
                log.LogError(e.Error.Message);
            }

        }


    }
}
