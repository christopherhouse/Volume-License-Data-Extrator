using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using InvoiceDataExtraction.Models.Requests;
using InvoiceDataExtraction.Models.Responses;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace InvoiceDataExtraction;

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