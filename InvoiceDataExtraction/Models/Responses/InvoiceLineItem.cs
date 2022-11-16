namespace InvoiceDataExtraction.Models.Responses;

public class InvoiceLineItem
{
    public decimal? LineItemAdditionalCharge { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? ExtendedAmount { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? QuantityOrdered { get; set; }

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
}