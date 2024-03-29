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
    private static readonly string _formsRecognizerEndpoint = Environment.GetEnvironmentVariable("formsRecognizerEndpoint");
    private static readonly string _formsRecognizerKey = Environment.GetEnvironmentVariable("formsRecognizerKey");
    private static readonly string _formsRecognizerModelId = Environment.GetEnvironmentVariable("modelId");

    public ExtractInvoiceDataActivity(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    [FunctionName(Constants.FunctionNames.ExtractInvoiceDataActivity)]
    public async Task<ExtractInvoiceDataResponse> ExtractInvoiceData([ActivityTrigger] ExtractInvoiceDataRequest request, ILogger log)
    {
        ExtractInvoiceDataResponse invoiceData;

        if (ValidateConfiguration())
        {
            var credential = new AzureKeyCredential(_formsRecognizerKey);

            var documentAnalysisClient = new DocumentAnalysisClient(new Uri(_formsRecognizerEndpoint), credential);

            try
            {
                var operation = await documentAnalysisClient.AnalyzeDocumentFromUriAsync(WaitUntil.Completed,
                    _formsRecognizerModelId,
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

    private bool ValidateConfiguration()
    {
        return !string.IsNullOrWhiteSpace(_formsRecognizerEndpoint) && !string.IsNullOrWhiteSpace(_formsRecognizerKey) &&
               !string.IsNullOrWhiteSpace(_formsRecognizerModelId);
    }
}