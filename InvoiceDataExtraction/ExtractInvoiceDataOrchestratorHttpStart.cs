using System.Net.Http;
using System.Threading.Tasks;
using InvoiceDataExtraction.Models.Requests;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InvoiceDataExtraction;

public static class ExtractInvoiceDataOrchestratorHttpStart
{
    [FunctionName("ExtractInvoiceDataOrchestrator_HttpStart")]
    public static async Task<HttpResponseMessage> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        var jsonBody = await req.Content.ReadAsStringAsync();
        var extractRequest = JsonConvert.DeserializeObject<ExtractInvoiceDataRequest>(jsonBody);
        var instance =
            await starter.StartNewAsync(Constants.FunctionNames.OrchestratorFunction, null, extractRequest);
            
        log.LogInformation($"Started orchestration with ID = '{instance}'.");

        return starter.CreateCheckStatusResponse(req, instance);
    }
}