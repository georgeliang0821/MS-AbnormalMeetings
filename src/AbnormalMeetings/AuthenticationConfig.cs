// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Azure.Storage.Blobs;
using System.Text;
using Directory = System.IO.Directory;

namespace daemon_console
{
    public class GlobalFunction
    {
        public static async Task SaveObjectToBlob(string filename, object jsonObject, string connectionString_name, string containerName_name, ILogger log)
        {
            string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
            string containerName = Environment.GetEnvironmentVariable("BlobContainerName_CallRecords");

            string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonObject);
            log.LogInformation("jsonString: " + jsonString);

            log.LogInformation("Writing file...");
            await daemon_console.GlobalFunction.SaveToBlob(filename, jsonString, connectionString, containerName, log);
            log.LogInformation("Success writing file: " + filename);
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
            container.CreateIfNotExists();

            var blob = container.GetBlobClient(file_name);
            using (MemoryStream mem = new MemoryStream())
            {
                // Write to stream
                Byte[] info = new UTF8Encoding(true).GetBytes(jsonString);
                mem.Write(info, 0, info.Length);

                // Go back to beginning of stream
                mem.Position = 0;

                // TODO: Handle the BlobAlreadyExists code
                await blob.UploadAsync(mem, overwrite: true);
            }

        }
        public static async Task ReadFromBlob(string file_name, string connectionString, string containerName, ILogger log)
        {
            //string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
            //string containerName = Environment.GetEnvironmentVariable("BlobContainerName");


            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            BlobClient blobClient = containerClient.GetBlobClient(file_name);

            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadAsync();
                using (var streamReader = new StreamReader(response.Value.Content))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();
                        Console.WriteLine(line);
                    }
                }
            }

            BlobContainerClient container = new BlobContainerClient(connectionString, containerName);

            //var blob = container.GetBlobClient(file_name);
            //using (MemoryStream mem = new MemoryStream())
            //{
            //    // Write to stream
            //    Byte[] info = new UTF8Encoding(true).GetBytes(jsonString);
            //    mem.Write(info, 0, info.Length);

            //    // Go back to beginning of stream
            //    mem.Position = 0;

            //    // TODO: Handle the BlobAlreadyExists code
            //    await blob.UploadAsync(mem, overwrite: true);
            //}

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

    /// <summary>
    /// Description of the configuration of an AzureAD public client application (desktop/mobile application). This should
    /// match the application registration done in the Azure portal
    /// </summary>
    public class AuthenticationConfig
    {
        /// <summary>
        /// instance of Azure AD, for example public Azure or a Sovereign cloud (Azure China, Germany, US government, etc ...)
        /// </summary>
        public string Instance { get; set; } = "https://login.microsoftonline.com/{0}";
       
        /// <summary>
        /// Graph API endpoint, could be public Azure (default) or a Sovereign cloud (US government, etc ...)
        /// </summary>
        public string ApiUrl { get; set; } = "https://graph.microsoft.com/";

        /// <summary>
        /// The Tenant is:
        /// - either the tenant ID of the Azure AD tenant in which this application is registered (a guid)
        /// or a domain name associated with the tenant
        /// - or 'organizations' (for a multi-tenant application)
        /// </summary>
        public string Tenant { get; set; }

        /// <summary>
        /// Guid used by the application to uniquely identify itself to Azure AD
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// URL of the authority
        /// </summary>
        public string Authority
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, Instance, Tenant);
            }
        }

        /// <summary>
        /// Client secret (application password)
        /// </summary>
        /// <remarks>Daemon applications can authenticate with AAD through two mechanisms: ClientSecret
        /// (which is a kind of application password: this property)
        /// or a certificate previously shared with AzureAD during the application registration 
        /// (and identified by the Certificate property belows)
        /// <remarks> 
        public string ClientSecret { get; set; }

        /// <summary>
        /// The description of the certificate to be used to authenticate your application.
        /// </summary>
        /// <remarks>Daemon applications can authenticate with AAD through two mechanisms: ClientSecret
        /// (which is a kind of application password: the property above)
        /// or a certificate previously shared with AzureAD during the application registration 
        /// (and identified by this CertificateDescription)
        /// <remarks> 
        public CertificateDescription Certificate { get; set; }

        /// <summary>
        /// Reads the configuration from a json file
        /// </summary>
        /// <param name="path">Path to the configuration json file</param>
        /// <returns>AuthenticationConfig read from the json file</returns>
        public static AuthenticationConfig ReadFromJsonFile(string path)
        {
            IConfigurationRoot Configuration;

            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path);

            Configuration = builder.Build();
            return Configuration.Get<AuthenticationConfig>();
        }
    }



}

