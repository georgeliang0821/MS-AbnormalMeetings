using System;
using daemon_console;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using System.IO;
using Newtonsoft.Json;
using global_class;

namespace CallRecordsSubscription
{
    public class RenewSubscription
    {
        [FunctionName("RenewSubscription")]
        public static async Task Run(
            [TimerTrigger("0 25 16 * * *")] TimerInfo myTimer, 
            ILogger log)
        {
            log.LogInformation($"RenewSubscription executed at: {DateTime.Now}");

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
                log.LogInformation("Running Function: CallMSGraphUsingGraphSDKAsync");
                await CallMSGraphUsingGraphSDKAsync(config, app, scopes, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static async Task CallMSGraphUsingGraphSDKAsync(AuthenticationConfig config, IConfidentialClientApplication app, string[] scopes, ILogger log)
        {
            SubscriptionList subscriptionList;
            
            string webhook_CallRecords = Environment.GetEnvironmentVariable("Webhook_CallRecords");
            string webhook_UserEvents = Environment.GetEnvironmentVariable("Webhook_UserEvents");
            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
            string containerName = Environment.GetEnvironmentVariable("BlobContainerName_SubscriptionList");
            string filename = Environment.GetEnvironmentVariable("BlobFileName");

            log.LogInformation("webhook_CallRecords: " + webhook_CallRecords);
            log.LogInformation("webhook_UserEvents: " + webhook_UserEvents);

            // Read file from blob
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            // use this code to create the container directly, if it does not exist.
            containerClient.CreateIfNotExists();

            BlobClient blobClient = containerClient.GetBlobClient(filename);

            log.LogInformation("Getting file in azure blob.");
            Dictionary<string, string> subscriptionDict = new Dictionary<string, string>();
            
            // If the file does not exist in azure, create a dummy one
            if (!await blobClient.ExistsAsync())
            {
                log.LogInformation("The target file do not exist in the container!");
                log.LogInformation("Creating a default json file for you!");

                //string jsonString = System.Text.Json.JsonSerializer.Serialize(subscriptionList);
                string jsonString = "{\"value\":[]}";
                await daemon_console.GlobalFunction.SaveToBlob(filename, jsonString, connectionString, containerName, log);
                log.LogInformation("Successfully creating a default json file.");
            }

            // Read the file in azure blob
            var response = await blobClient.DownloadAsync();
            var responseStream = response.Value.Content;
            string responseString = await new StreamReader(responseStream).ReadToEndAsync();

            subscriptionList = JsonConvert.DeserializeObject<SubscriptionList>(responseString);
            if (subscriptionList.value == null)
            {
                subscriptionList.value = new List<SubscriptionInfo>();
            }
            else
            {
                foreach (var subscriptionInfo in subscriptionList.value)
                {
                    subscriptionDict.Add(subscriptionInfo.UserId, subscriptionInfo.SubscriptionId);
                    // log.LogInformation(subscriptionInfo.UserId + " : " + subscriptionDict[subscriptionInfo.UserId]);
                }
            }


            ////////////////////////////////////////////////////////
            ////// Start to create or renew the subscription////////
            ////////////////////////////////////////////////////////
            IGraphServiceUsersCollectionPage users = null;
            try
            { 
                GraphServiceClient graphServiceClient = daemon_console.GlobalFunction.GetAuthenticatedGraphClient(app, scopes);

                // Print all subscription in the current tenant
                log.LogInformation("Printing all subscription in the current tenant");
                var first_sub = await graphServiceClient.Subscriptions.Request().GetAsync();
                while (first_sub != null)
                {
                    foreach (var sub in first_sub)
                    {
                        log.LogInformation("Subscription ID: " + sub.Id + "\tExpirationDateTime: " + sub.ExpirationDateTime + "\tResource: " + sub.Resource);
                    }
                    try
                    {
                         first_sub = await first_sub.NextPageRequest.GetAsync();

                    }
                    catch (NullReferenceException)
                    {
                        log.LogInformation("No next link exist");
                        first_sub = null;
                    }
                }

                // Double timeShift = 1440 * 3 - 10;
                Double timeShift = 1440 * 2;
                DateTime expirationDateTime = DateTime.UtcNow.AddMinutes(timeShift);

                ////////////////////////////////////////////////////////
                /////// Renew or Create CallRecord subscription ////////
                ////////////////////////////////////////////////////////
                string callRecordId = "callRecordId";
                log.LogInformation("Renew or Create CallRecord subscription");
                // Renew
                if (subscriptionDict.ContainsKey(callRecordId))
                {
                    await GraphApi_RenewSubscription(graphServiceClient, subscriptionDict[callRecordId], expirationDateTime, log);
                }
                // Create
                else
                {
                    var subscription = new Subscription
                    {
                        ChangeType = "created",
                        NotificationUrl = webhook_CallRecords,
                        Resource = "/communications/callRecords",
                        ExpirationDateTime = expirationDateTime,
                        ClientState = "secretClientValue",
                        LatestSupportedTlsVersion = "v1_2"
                    };

                    Subscription responseSubscription = await GraphApi_CreateSubscription(graphServiceClient, subscription, log);

                    if (responseSubscription != null)
                    {
                        SubscriptionInfo subscriptionInfo = new SubscriptionInfo(callRecordId, responseSubscription.Id);
                        subscriptionList.value.Add(subscriptionInfo);
                    }
                }

                ////////////////////////////////////////////////////////
                /////// Renew or Create userEvents subscription ////////
                ////////////////////////////////////////////////////////
                // Renew or Create user subscription
                log.LogInformation("Renew or Create UserEvents subscription");
                users = await graphServiceClient.Users.Request().GetAsync();

                log.LogInformation("Found # of user: " + users.Count);

                foreach (var user in users)
                {
                    log.LogInformation("Current user ID: " + user.Id);
                    
                    // Renew
                    if (subscriptionDict.ContainsKey(user.Id))
                    {

                        await GraphApi_RenewSubscription(graphServiceClient, subscriptionDict[user.Id], expirationDateTime, log);
                    }
                    // Create
                    else
                    {
                        var subscription = new Subscription
                        {
                            ChangeType = "created,updated",
                            NotificationUrl = webhook_UserEvents,
                            Resource = "/users/" + user.Id + "/events",
                            ExpirationDateTime = expirationDateTime,
                            ClientState = "secretClientValue",
                            LatestSupportedTlsVersion = "v1_2"
                        };

                        Subscription responseSubscription = await GraphApi_CreateSubscription(graphServiceClient, subscription, log);
                     
                        if (responseSubscription != null)
                        {
                            SubscriptionInfo subscriptionInfo = new SubscriptionInfo(user.Id, responseSubscription.Id);
                            subscriptionList.value.Add(subscriptionInfo);
                        }                        
                    }
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

        private static async Task GraphApi_RenewSubscription(GraphServiceClient graphServiceClient, string subscriptionId, DateTimeOffset expirationDateTime, ILogger log)
        {
            try
            {
                log.LogInformation("\tRenewing a subscription");
                var subscription = new Subscription
                {
                    ExpirationDateTime = expirationDateTime,
                };

                //string subscriptionId = Environment.GetEnvironmentVariable("subscriptionId");
                log.LogInformation(String.Format("\t\tTry to renew: subscriptionId- {0}; ExpirationDateTime- {1}: ", subscriptionId, expirationDateTime));

                await graphServiceClient.Subscriptions[subscriptionId].Request().UpdateAsync(subscription);
                log.LogInformation("\t\tSuccessfully update the subscription!");
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static async Task<Subscription> GraphApi_CreateSubscription(GraphServiceClient graphServiceClient, Subscription subscription, ILogger log)
        {
            log.LogInformation("\tCreating a subscription");

            try
            {
                Subscription responseSubscription = await graphServiceClient.Subscriptions
                .Request()
                .AddAsync(subscription);

                log.LogInformation("\t\tSuccessfully create the subscription");
                // log.LogInformation("\t\tUserId: " + user.Id);

                return responseSubscription;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogInformation("\tThere is an error when processing events! (May be MailboxNotEnabledForRESTAPI)");
                // log.LogInformation("\tUserId: " + user.Id);

                return null;
            }
        }


    }
}
