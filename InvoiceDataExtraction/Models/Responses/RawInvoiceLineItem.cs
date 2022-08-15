using Azure.AI.FormRecognizer.DocumentAnalysis;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace InvoiceDataExtraction.Models.Responses;

public class RawInvoiceLineItem
{
    public string LineItemDiscount { get; set; }

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
            LineItemDiscount = ParseDecimal(this.LineItemDiscount, nameof(LineItemDiscount), this.LineNumber)
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
                    break;
                case "Unit Price":
                    lineItem.UnitPrice = content;
                    break;
                case "Extended Amount":
                    lineItem.ExtendedAmount = content;
                    break;
                case "Tax Amount":
                    lineItem.TaxAmount = content;
                    break;
                case "Line Item Discount":
                    lineItem.LineItemDiscount = content;
                    break;
                default:
                    break;
            }
        }

        return lineItem;
    }

    private static decimal GetQuantityOrdered(DocumentField field, string lineNumber)
    {
        if (decimal.TryParse(field.Content, out var decimalQuantity))
        {
            return decimalQuantity;
        }
        else
        {
            throw new FormatException(
                $"Could not convert Quantity Ordered to integer on line number {lineNumber}.  Input value was {field.Content}");
        }
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

    private static decimal ParseDecimal(string content, string fieldName, string lineNumber)
    {
        var fieldValue = Regex.Replace(content, @"\p{C}+", string.Empty);

        if (fieldValue.Contains("62,361.22"))
        {
            // problem
            var thing = Regex.Replace(fieldValue, @"\p{C}+", string.Empty);
            Console.WriteLine(thing);
        }

        var culture = CultureInfo.CreateSpecificCulture("en-US");


        if (decimal.TryParse(fieldValue, NumberStyles.Any, culture, out var parsedDecimal))
        {
            return parsedDecimal;
        }
        else
        {
            throw new FormatException(
                $"Unable to parse decimal value from field {fieldName}, line number {lineNumber}.  Input value was {content}");
        }
    }
}