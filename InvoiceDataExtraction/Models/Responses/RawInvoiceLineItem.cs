using Azure.AI.FormRecognizer.DocumentAnalysis;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System;

namespace InvoiceDataExtraction.Models.Responses;

public class RawInvoiceLineItem
{
    public string LineItemAdditionalCharge { get; set; }

    public string TaxAmount { get; set; }

    public string ExtendedAmount { get; set; }

    public string UnitPrice { get; set; }

    public string QuantityOrdered { get; set; }

    public string Taxable { get; set; }

    public string BillingOption { get; set; }

    public string Delivery { get; set; }

    public string Period { get; set; }

    public string Pool { get; set; }

    public string LicenseType { get; set; }

    public string Description { get; set; }

    public string MicrosoftPartNumber { get; set; }

    public string UsageCountry { get; set; }

    public string LineNumber { get; set; }

    public float? UnitPriceConfidence { get; set; }

    public float? ExtendedAmountConfidence { get; set; }

    public float? QuantityOrderedConfidence { get; set; }

    public float? LineItemAdditionalChargeConfidence { get; set; }
    
    public float? LineNumberConfidence { get; set; }

    public InvoiceLineItem AsInvoiceLineItem()
    {
        return new InvoiceLineItem
        {
            LineNumber = this.LineNumber,
            UsageCountry = this.UsageCountry,
            MicrosoftPartNumber = this.MicrosoftPartNumber,
            Description = this.Description,
            LicenseType = this.LicenseType,
            Pool = this.Pool,
            Period = this.Period,
            Delivery = this.Delivery,
            BillingOption = this.BillingOption,
            Taxable = this.Taxable,
            QuantityOrdered = ParseDecimal(this.QuantityOrdered, nameof(QuantityOrdered), this.LineNumber),
            UnitPrice = ParseDecimal(this.UnitPrice, nameof(UnitPrice), this.LineNumber),
            ExtendedAmount = ParseDecimal(this.ExtendedAmount, nameof(ExtendedAmount), this.LineNumber),
            TaxAmount = ParseDecimal(this.TaxAmount, nameof(TaxAmount), this.LineNumber),
            LineItemAdditionalCharge = ParseDecimal(this.LineItemAdditionalCharge, nameof(LineItemAdditionalCharge), this.LineNumber),
            UnitPriceConfidence = this.UnitPriceConfidence,
            ExtendedAmountConfidence = this.ExtendedAmountConfidence,
            QuantityOrderedConfidence = this.QuantityOrderedConfidence,
            LineItemAdditionalChargeConfidence = this.LineItemAdditionalChargeConfidence,
            LineNumberConfidence = this.LineNumberConfidence
        };
    }

    public static RawInvoiceLineItem FromDocumentField(DocumentField lineItems)
    {
        var lineItem = new RawInvoiceLineItem();

        var fieldDictionary = lineItems.AsDictionary();
        var lineNumber = GetLineNumber(fieldDictionary);

        foreach (var field in fieldDictionary)
        {
            var content = field.Value.Content;
            switch (field.Key)
            {
                case "Line Number":
                    lineItem.LineNumber = lineNumber;
                    lineItem.LineNumberConfidence = field.Value.Confidence;
                    break;
                case "Usage Country":
                    lineItem.UsageCountry = content;
                    break;
                case "Microsoft Part Number":
                    lineItem.MicrosoftPartNumber = content;
                    break;
                case "Description":
                    lineItem.Description = content;
                    break;
                case "License Type Level":
                    lineItem.LicenseType = content;
                    break;
                case "Pool":
                    lineItem.Pool = content;
                    break;
                case "Period":
                    lineItem.Period = content;
                    break;
                case "Delivery":
                    lineItem.Delivery = content;
                    break;
                case "Billing Option":
                    lineItem.BillingOption = content;
                    break;
                case "Taxable":
                    lineItem.Taxable = content;
                    break;
                case "Quantity Ordered":
                    lineItem.QuantityOrdered = content;
                    lineItem.QuantityOrderedConfidence = field.Value.Confidence;
                    break;
                case "Unit Price":
                    lineItem.UnitPrice = content;
                    lineItem.UnitPriceConfidence = field.Value.Confidence;
                    break;
                case "Extended Amount":
                    lineItem.ExtendedAmount = content;
                    lineItem.ExtendedAmountConfidence = field.Value.Confidence;
                    break;
                case "Tax Amount":
                    lineItem.TaxAmount = content;
                    break;
                case "Line Item Additional Charge":
                    lineItem.LineItemAdditionalCharge = content;
                    lineItem.LineItemAdditionalChargeConfidence = field.Value.Confidence;
                    break;
                default:
                    Console.WriteLine($"Unhandled field encountered: {field.Key}");
                    break;
            }
        }

        return lineItem;
    }

    private static string GetLineNumber(IReadOnlyDictionary<string, DocumentField> fields)
    {
        string lineNumber = "UNKNOWN";
        if (fields.ContainsKey("Line Number"))
        {
            lineNumber = fields["Line Number"].Content;
        }

        return lineNumber;
    }

    private static decimal? ParseDecimal(string content, string fieldName, string lineNumber)
    {
        var parsedValue = default(decimal?);

        var culture = CultureInfo.CreateSpecificCulture("en-US");

        var fieldValue = string.IsNullOrWhiteSpace(content) ? default(decimal).ToString(culture) : Regex.Replace(content, @"\p{C}+", string.Empty);

        if (decimal.TryParse(fieldValue, NumberStyles.Any, culture, out var parsedDecimal))
        {
            parsedValue = parsedDecimal;
        }
        else
        {
            Console.WriteLine(
                $"Unable to parse decimal value from field {fieldName}, line number {lineNumber}.  Input value was {content}");
        }

        return parsedValue;
    }
}