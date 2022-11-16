using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using InvoiceDataExtraction.Models.Requests;
using InvoiceDataExtraction.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace InvoiceDataExtraction;

public class ExtractInvoiceData
{
    private const string MODEL_ID = "invoices-2022-08-10-1536";

    private readonly ILogger<ExtractInvoiceData> _logger;

    public ExtractInvoiceData(ILogger<ExtractInvoiceData> logger)
    {
        _logger = logger ?? throw new ArgumentException(nameof(logger));
    }

    [FunctionName("ExtractInvoiceData")]
    [OpenApiOperation(operationId: "ExtractInvoiceData", tags: new[] { "name" })]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody("application/json", typeof(ExtractInvoiceDataRequest), Description = "Request body")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ExtractInvoiceDataResponse), Description = "The OK response")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, "application/json", typeof(ErrorResponse), Description = "Bad Request")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        IActionResult result = null;
        var endpoint = Environment.GetEnvironmentVariable("formsRecognizerEndpoint");
        var key = Environment.GetEnvironmentVariable("formsRecogniserKey");

        if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(key))
        {
            var credential = new AzureKeyCredential(key);
            
            var documentAnalysisClient = new DocumentAnalysisClient(new Uri(endpoint), credential);
            ExtractInvoiceDataRequest request = null;

            try
            {
                using (var reader = new StreamReader(req.Body))
                {
                    var bodyJson = await reader.ReadToEndAsync();
                    request = ExtractInvoiceDataRequest.FromJsonString(bodyJson);
                }

                var operation = await documentAnalysisClient.AnalyzeDocumentFromUriAsync(WaitUntil.Completed,
                    MODEL_ID,
                    request.InvoiceSasUri);

                var results = operation.Value;

                var document = results.Documents.First();
                var fields = document.Fields;

                var response = ExtractInvoiceDataResponse.FromFormFields(fields);
                result = new OkObjectResult(response);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogError(e, e.Message);
                result = new BadRequestObjectResult(new ErrorResponse { ErrorMessage = e.ToString() });
            }
        }
        else
        {
            result = new StatusCodeResult(500);
        }


        return result;
    }
}