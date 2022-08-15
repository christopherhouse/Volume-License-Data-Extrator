using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure;
using InvoiceDataExtraction.Models.Requests;
using InvoiceDataExtraction.Models.Responses;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InvoiceDataExtraction
{
    public static class Constants
    {
        public static class FunctionNames
        {
            public const string OrchestratorFunction = "ExtractInvoiceDataOrchestrator";
            public const string ExtractInvoiceDataActivity = "ExtractInvoiceDataActivity";
        }
    }

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

    public class ExtractInvoiceDataActivity
    {
        private const string MODEL_ID = "invoices-2022-08-10-1536";

        [FunctionName(Constants.FunctionNames.ExtractInvoiceDataActivity)]
        public static async Task<ExtractInvoiceDataResponse> ExtractInvoiceData([ActivityTrigger] ExtractInvoiceDataRequest request, ILogger log)
        {
            ExtractInvoiceDataResponse invoiceData = null;
            var endpoint = Environment.GetEnvironmentVariable("formsRecognizerEndpoint");
            var key = Environment.GetEnvironmentVariable("formsRecogniserKey");

            if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(key))
            {
                var credential = new AzureKeyCredential(key);

                var documentAnalysisClient = new DocumentAnalysisClient(new Uri(endpoint), credential);

                try
                {
                    var operation = await documentAnalysisClient.AnalyzeDocumentFromUriAsync(WaitUntil.Completed,
                        MODEL_ID,
                        request.InvoiceSasUri);

                    var results = operation.Value;

                    var document = results.Documents.First();
                    var fields = document.Fields;

                    invoiceData = ExtractInvoiceDataResponse.FromFormFields(fields);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    log.LogError(e, e.Message);
                    throw;
                }
            }
            else
            {
                throw new ApplicationException("Missing configuration");
            }

            return invoiceData;
        }
    }
}