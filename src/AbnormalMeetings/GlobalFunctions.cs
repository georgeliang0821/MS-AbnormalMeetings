using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Azure.Storage.Blobs;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net.Http;

namespace daemon_console
{
    public class GlobalFunction
    {
        /// <summary>
        /// Print headers of the http request to log
        /// </summary>
        /// <param name="req_Headers"></param>
        /// <param name="log"></param>
        public static void PrintHeaders(IHeaderDictionary req_Headers, ILogger log)
        {
            string str_headers = "The Headers are: \n";

            var zipItems = req_Headers.Keys.Zip(
                req_Headers.Values, (first, second) 
                    => "Key:" + first + "\tValue:" + second + "\n");

            foreach (var item in zipItems)
                str_headers += item;

            log.LogInformation(str_headers);
        }

        /// <summary>
        /// The following example shows how to initialize the MS Graph SDK
        /// </summary>
        /// <param name="app"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static async Task<string> GetHttpRequest(IConfidentialClientApplication app, string[] scopes, string webApiUrl, ILogger log)
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

        /// <summary>
        /// An example of how to authenticate the Microsoft Graph SDK using the MSAL library
        /// </summary>
        /// <returns></returns>
        public static GraphServiceClient GetAuthenticatedGraphClient(IConfidentialClientApplication app, string[] scopes)
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
        /// Get logined App
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IConfidentialClientApplication GetAppAsync(AuthenticationConfig config)
        {
            IConfidentialClientApplication app;

            // Even if this is a console application here, a daemon application is a confidential client application
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                .WithClientSecret(config.ClientSecret)
                .WithAuthority(new Uri(config.Authority))
                .Build();

            _ = app.AddInMemoryTokenCache();

            return app;
        }

        /// <summary>
        /// Save event to blob
        /// </summary>
        /// <param name="file_name"></param>
        /// <param name="jsonString"></param>
        /// <param name="connectionString"></param>
        /// <param name="containerName"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task SaveToBlob(string file_name, string jsonString, string connectionString, string containerName, ILogger log)
        {

            BlobContainerClient container = new BlobContainerClient(connectionString, containerName);
            _ = container.CreateIfNotExists();

            var blob = container.GetBlobClient(file_name);
            using (MemoryStream mem = new MemoryStream())
            {
                // Write to stream
                Byte[] info = new UTF8Encoding(true).GetBytes(jsonString);
                mem.Write(info, 0, info.Length);

                // Go back to beginning of stream
                mem.Position = 0;

                // Upload the file to the server
                _ = await blob.UploadAsync(mem, overwrite: true);
                await blob.UploadAsync(mem, overwrite: true);
            }

        }

        ///// <summary>
        ///// Calls MS Graph REST API using an authenticated Http client
        ///// </summary>
        ///// <param name="config"></param>
        ///// <param name="app"></param>
        ///// <param name="scopes"></param>
        ///// <returns></returns>
        //private static async Task CallMSGraph(AuthenticationConfig config, IConfidentialClientApplication app, string[] scopes, string meetingID)
        //{
        //    AuthenticationResult result = null;
        //    try
        //    {
        //        result = await app.AcquireTokenForClient(scopes)
        //            .ExecuteAsync();

        //        Console.ForegroundColor = ConsoleColor.Green;
        //        Console.WriteLine("Token acquired");
        //        Console.ResetColor();
        //    }
        //    catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
        //    {
        //        // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
        //        // Mitigation: change the scope to be as expected
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.WriteLine("Scope provided is not supported");
        //        Console.ResetColor();
        //    }

        //    // The following example uses a Raw Http call 
        //    if (result != null)
        //    {
        //        var httpClient = new HttpClient();
        //        var apiCaller = new ProtectedApiCallHelper(httpClient);
        //        //await apiCaller.CallWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/users", result.AccessToken, Display);
        //        Console.WriteLine("Running: await apiCaller.CallWebApiAndProcessResultASync");
        //        await apiCaller.CallWebApiAndProcessResultASync(
        //            $"{config.ApiUrl}v1.0/communications/callRecords/{meetingID}",
        //            result.AccessToken, Display);
        //            //$"{config.ApiUrl}v1.0/communications/callRecords/{meetingID}?$expand=sessions($expand=segments)",
        //    }
        //}
    }
}