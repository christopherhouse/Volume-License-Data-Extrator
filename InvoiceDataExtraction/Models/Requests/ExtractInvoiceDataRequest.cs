using System;
using Newtonsoft.Json;

namespace InvoiceDataExtraction.Models.Requests;

public class ExtractInvoiceDataRequest
{
    public Uri InvoiceSasUri { get; set; }

    public static ExtractInvoiceDataRequest FromJsonString(string jsonBody)
    {
        return JsonConvert.DeserializeObject<ExtractInvoiceDataRequest>(jsonBody);
    }
}