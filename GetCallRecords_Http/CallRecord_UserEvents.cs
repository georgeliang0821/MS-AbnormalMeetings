using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using daemon_console;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace CallRecord_UserEvents
{
    public class CallRecord_UserEvents
    {
        [FunctionName("CallRecord_UserEvents")]
        //public void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        public void Run([TimerTrigger("0 0 6 * * *")]TimerInfo myTimer, ILogger log)
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

                log.LogInformation("Getting call records...");

                string[] scopes = new string[] { $"{config.ApiUrl}.default" }; // Generates a scope -> "https://graph.microsoft.com/.default"

                // Call MS graph using the Graph SDK
                log.LogInformation("Running Function: CallMSGraphUsingGraphSDK");
                _ = SaveUserEvents(config, app, scopes, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private async Task SaveUserEvents(AuthenticationConfig config, IConfidentialClientApplication app, string[] scopes, ILogger log)
        {
            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
            string containerName = Environment.GetEnvironmentVariable("BlobContainerName_UserEvents");
            
            Double timeRange = 1440 * 1;
            Double timeShift = -1440 * 1;
            String targetStartDatetime = "'" + DateTime.UtcNow.AddMinutes(timeShift).ToString("s") + "'";
            String targetEndDatetime = "'" + DateTime.UtcNow.AddMinutes(timeShift + timeRange).ToString("s") + "'";

            string queryString = "start/dateTime ge " + targetStartDatetime + " and " + "end/dateTime le " + targetEndDatetime;
            log.LogInformation("queryString: " + queryString);

            try
            {
                GraphServiceClient graphServiceClient = daemon_console.GlobalFunction.GetAuthenticatedGraphClient(app, scopes);
                
                var users = await graphServiceClient.Users.Request().GetAsync();
                log.LogInformation("Found # of user: " + users.Count);

                foreach (var user in users)
                {

                    log.LogInformation("Current user ID: " + user.Id);

                    // TODO: catch the exact problem
                    try
                    {
                        var events = await graphServiceClient.Users[user.Id].Events
                            .Request()
                            .Filter(queryString)
                            .GetAsync();

                        foreach (var event_ in events)
                        {
                            log.LogInformation("\tsubject: " + event_.Subject);
                            string filename = event_.Id + ".json";
                            string jsonString = System.Text.Json.JsonSerializer.Serialize(event_);

                            await daemon_console.GlobalFunction.SaveToBlob(filename, jsonString, connectionString, containerName, log);
                            log.LogInformation("\tsubject: " + event_.Subject + " Save to: " + filename);
                            //log.LogInformation("\tStart time: " + event_.Start.DateTime);
                            //log.LogInformation("\tEnd time: " + event_.End.DateTime);
                            //foreach (var attendee in event_.Attendees)
                            //{
                            //    log.LogInformation("\t\temailAddress: " + attendee.EmailAddress.Address);
                            //}
                        }
                    }
                    catch
                    {
                        log.LogInformation("\tthere is an error when processing events! (May be MailboxNotEnabledForRESTAPI)");
                    }
                    
                }
            }
            catch (ServiceException e)
            {
                log.LogError(e.Error.Message);
            }
        }
    }
}
