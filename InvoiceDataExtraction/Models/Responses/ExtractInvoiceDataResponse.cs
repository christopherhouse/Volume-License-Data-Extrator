using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace InvoiceDataExtraction.Models.Responses;

public class ExtractInvoiceDataResponse
{
    public ExtractInvoiceDataResponse()
    {
        LineItems = new List<InvoiceLineItem>();
        UnparsedLineItems = new List<RawInvoiceLineItem>();
    }

    public string InvoiceNumber { get; set; }

    public float? InvoiceNumberConfidence { get; set; }

    public decimal? ExtractedInvoiceTotal { get; set; }

    public float? ExtractedInvoiceTotalConfidence { get; set; }

    public decimal? ComputedInvoiceTotal { get; set; }

    public bool ExtractedValuesMatchComputed { get; set; }

    public string EnrollmentNumber { get; set; }

    public float? EnrollmentNumberConfidence { get; set; }

    public string InvoiceDate { get; set; }

    public float? InvoiceDateConfidence { get; set; }

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
                    response.InvoiceNumberConfidence = field.Value.Confidence;
                    break;
                case "Invoice Total":
                    response.ExtractedInvoiceTotal = ParseDecimal(field.Value.Content, nameof(ExtractedInvoiceTotal));
                    response.ExtractedInvoiceTotalConfidence = field.Value.Confidence;
                    break;
                case "Line Items":
                    var lineItems = field.Value.Value.AsList();

                    foreach (var lineItem in lineItems)
                    {
                        var rawLineItem = RawInvoiceLineItem.FromDocumentField(lineItem);

                        try
                        {
                            var parsedLineItem = rawLineItem.AsInvoiceLineItem();
                            response.LineItems.Add(parsedLineItem);
                        }
                        catch (InvalidOperationException)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                            Console.WriteLine($"Adding raw line item to UnparsedLineItems");
                            response.UnparsedLineItems.Add(rawLineItem);
                        }
                    }

                    break;
                case "Enrollment Number":
                    response.EnrollmentNumber = field.Value.Content;
                    response.EnrollmentNumberConfidence = field.Value.Confidence;
                    break;
                case "Invoice Date":
                    response.InvoiceDate = field.Value.Content;
                    response.InvoiceDateConfidence = field.Value.Confidence;
                    break;
                default:
                    break;
            }
        }

        response.ComputedInvoiceTotal =
            response.LineItems.Sum(_ => _.ExtendedAmount + (_.LineItemAdditionalCharge ?? default(decimal)));

        response.ExtractedValuesMatchComputed = response.ComputedInvoiceTotal == response.ExtractedInvoiceTotal;

        return response;
    }

    private static decimal? ParseDecimal(string content, string fieldName)
    {
        var culture = CultureInfo.CreateSpecificCulture("en-US");
        var fieldValue = string.IsNullOrWhiteSpace(content) ? default(decimal).ToString(culture) : Regex.Replace(content, @"\p{C}+", string.Empty);
        var parsedValue = default(decimal?);


        if (decimal.TryParse(fieldValue, NumberStyles.Any, culture, out var parsedDecimal))
        {
            parsedValue = parsedDecimal;
        }
        else
        {
            Console.WriteLine(
                $"Unable to parse decimal value from field {fieldName}.  Input value was {content}");
        }

        return parsedValue;
    }
}