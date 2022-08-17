using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(InvoiceDataExtraction.Startup))]

namespace InvoiceDataExtraction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var connectionString = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_CONNECTION_STRING");
            builder.Services.AddApplicationInsightsTelemetry(_ => _.ConnectionString = connectionString);
        }
    }
}
