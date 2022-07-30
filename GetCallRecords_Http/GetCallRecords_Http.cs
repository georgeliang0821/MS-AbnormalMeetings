using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

            //return new OkResult();

            string responseMessage = "Token is token";
            return new OkObjectResult(responseMessage);

            //if (req.QueryString["validationToken"] != null)
            //{
            //    var token = Request.QueryString["validationToken"];
            //    return Content(token, "text/plain");
            //}

        }

    }
}

