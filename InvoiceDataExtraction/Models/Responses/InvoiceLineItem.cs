using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace InvoiceDataExtraction.Models.Responses;

public class InvoiceLineItem
{
    public decimal LineItemDiscount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ExtendedAmount { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal QuantityOrdered { get; set; }

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

    public static InvoiceLineItem FromDocumentField(DocumentField lineItemField)
    {
        var lineItem = new InvoiceLineItem();

        var fieldDictionary = lineItemField.AsDictionary();
        var lineNumber = GetLineNumber(fieldDictionary);

        foreach (var field in fieldDictionary)
        {
            switch (field.Key)
            {
                case "Line Number":
                    lineItem.LineNumber = lineNumber;
                    break;
                case "Usage Country":
                    lineItem.UsageCountry = field.Value.Content;
                    break;
                case "Microsoft Part Number":
                    lineItem.MicrosoftPartNumber = field.Value.Content;
                    break;
                case "Description":
                    lineItem.Description = field.Value.Content;
                    break;
                case "License Type Level":
                    lineItem.LicenseType = field.Value.Content;
                    break;
                case "Pool":
                    lineItem.Pool = field.Value.Content;
                    break;
                case "Period":
                    lineItem.Period = field.Value.Content;
                    break;
                case "Delivery":
                    lineItem.Delivery = field.Value.Content;
                    break;
                case "Billing Option":
                    lineItem.BillingOption = field.Value.Content;
                    break;
                case "Taxable":
                    lineItem.Taxable = field.Value.Content;
                    break;
                case "Quantity Ordered":
                    lineItem.QuantityOrdered = GetQuantityOrdered(field.Value, lineNumber);
                    break;
                case "Unit Price":
                    lineItem.UnitPrice = ParseDecimal(field, lineNumber);
                    break;
                case "Extended Amount":
                    lineItem.ExtendedAmount = ParseDecimal(field, lineNumber);
                    break;
                case "Tax Amount":
                    lineItem.TaxAmount = ParseDecimal(field, lineNumber);
                    break;
                case "Line Item Discount":
                    lineItem.LineItemDiscount = ParseDecimal(field, lineNumber);
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

    private static decimal ParseDecimal(KeyValuePair<string, DocumentField> field, string lineNumber)
    {
        var fieldValue = Regex.Replace(field.Value.Content, @"\p{C}+", string.Empty);

        if (fieldValue.Contains("62,361.22"))
        {
            // problem
            var thing = Regex.Replace(fieldValue, @"\p{C}+", string.Empty);
            Console.WriteLine(thing);
        }

        var culture = CultureInfo.CreateSpecificCulture("en-US");
        // Numbers can have commas, so need to allow them when parsing.  Also allowing currency symbols, in case that shows up
        // on a number we need to parse
        var numberStyle = NumberStyles.AllowThousands | NumberStyles.AllowCurrencySymbol;

        if (decimal.TryParse(fieldValue, numberStyle, culture, out var parsedDecimal))
        {
            return parsedDecimal;
        }
        else
        {
            throw new FormatException(
                $"Unable to parse decimal value from field {field.Key}, line number {lineNumber}.  Input value was {field.Value.Content}");
        }
    }
}