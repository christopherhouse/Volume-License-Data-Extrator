using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using InvoiceDataExtraction.Models.Requests;
using InvoiceDataExtraction.Models.Responses;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace InvoiceDataExtraction;

public class ExtractInvoiceDataActivity
{
    private readonly TelemetryClient _telemetryClient;

    public ExtractInvoiceDataActivity(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    [FunctionName(Constants.FunctionNames.ExtractInvoiceDataActivity)]
    public async Task<ExtractInvoiceDataResponse> ExtractInvoiceData([ActivityTrigger] ExtractInvoiceDataRequest request, ILogger log)
    {
        ExtractInvoiceDataResponse invoiceData;
        var endpoint = Environment.GetEnvironmentVariable("formsRecognizerEndpoint");
        var key = Environment.GetEnvironmentVariable("formsRecogniserKey");
        var modelId = Environment.GetEnvironmentVariable("modelId");

        if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(modelId))
        {
            var credential = new AzureKeyCredential(key);

            var documentAnalysisClient = new DocumentAnalysisClient(new Uri(endpoint), credential);

            try
            {
                var operation = await documentAnalysisClient.AnalyzeDocumentFromUriAsync(WaitUntil.Completed,
                    modelId,
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

        var @event = new EventTelemetry("Invoice Processed")
        {
            Properties =
            {
                {nameof(invoiceData.InvoiceNumber), invoiceData.InvoiceNumber},
                {nameof(invoiceData.ExtractedInvoiceTotal), invoiceData.ExtractedInvoiceTotal != null ? invoiceData.ExtractedInvoiceTotal.ToString() : "NULL"},
                {nameof(invoiceData.ComputedInvoiceTotal), invoiceData.ComputedInvoiceTotal != null ? invoiceData.ComputedInvoiceTotal.ToString() : "NULL"},
                {nameof(invoiceData.ExtractedValuesMatchComputed), invoiceData.ExtractedValuesMatchComputed.ToString()},
                {nameof(invoiceData.EnrollmentNumber), invoiceData.EnrollmentNumber}
            }
        };

        _telemetryClient.TrackEvent(@event);

        return invoiceData;
    }
}