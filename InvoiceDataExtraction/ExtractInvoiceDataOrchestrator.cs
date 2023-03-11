using System.Threading.Tasks;
using InvoiceDataExtraction.Models.Requests;
using InvoiceDataExtraction.Models.Responses;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace InvoiceDataExtraction;

public static class ExtractInvoiceDataOrchestrator
{
    [FunctionName(Constants.FunctionNames.OrchestratorFunction)]
    public static async Task<ExtractInvoiceDataResponse> RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var sasUrl = context.GetInput<ExtractInvoiceDataRequest>();
        var result = await context.CallActivityAsync<ExtractInvoiceDataResponse>(Constants.FunctionNames.ExtractInvoiceDataActivity, sasUrl);
        return result;
    }
}