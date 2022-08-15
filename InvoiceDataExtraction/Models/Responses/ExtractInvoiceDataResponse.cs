using System;
using System.Collections.Generic;
using System.Linq;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace InvoiceDataExtraction.Models.Responses
{
    public class ExtractInvoiceDataResponse
    {
        public ExtractInvoiceDataResponse()
        {
            LineItems = new List<InvoiceLineItem>();
            UnparsedLineItems = new List<RawInvoiceLineItem>();
        }

        public string InvoiceNumber { get; set; }

        public decimal? ExtractedInvoiceTotal { get; set; }

        public decimal? ComputedInvoiceTotal { get; set; }

        public bool ExtractedValuesMatchComputed { get; set; }

        public string EnrollmentNumber { get; set; }

        public IList<InvoiceLineItem> LineItems { get; }

        public IList<RawInvoiceLineItem> UnparsedLineItems { get; }

        public static ExtractInvoiceDataResponse FromFormFields(IReadOnlyDictionary<string, DocumentField> fields)
        {
            var response = new ExtractInvoiceDataResponse();

            foreach (var field in fields)
            {
                switch (field.Key)
                {
                    case "Invoice Number":
                        response.InvoiceNumber = field.Value.Content;
                        break;
                    case "Invoice Total":
                        response.ExtractedInvoiceTotal = decimal.Parse(field.Value.Content);
                        break;
                    case "Line Items":
                        var lineItems = field.Value.AsList();

                        foreach (var lineItem in lineItems)
                        {
                            //response.LineItems.Add(InvoiceLineItem.FromDocumentField(lineItem));
                            var rawLineItem = RawInvoiceLineItem.FromDocumentField(lineItem);

                            try
                            {
                                var parsedLineItem = rawLineItem.AsInvoiceLineItem();
                                response.LineItems.Add(parsedLineItem);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Adding raw line item to UnparsedLineItems");
                                response.UnparsedLineItems.Add(rawLineItem);
                            }
                        }

                        break;
                    case "Enrollment Number":
                        response.EnrollmentNumber = field.Value.Content;
                        break;
                    default:
                        break;
                }
            }

            response.ComputedInvoiceTotal = response.LineItems.Sum(_ => _.ExtendedAmount);
            response.ExtractedValuesMatchComputed = response.ComputedInvoiceTotal == response.ExtractedInvoiceTotal;

            return response;
        }
    }
}
