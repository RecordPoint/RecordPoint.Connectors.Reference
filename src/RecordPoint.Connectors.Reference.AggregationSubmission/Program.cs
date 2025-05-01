using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.ContentManager;
using RecordPoint.Connectors.SDK.R365;

namespace RecordPoint.Connectors.Reference.AggregationSubmission;

/// <summary>
/// The aggregation submission service submits aggregations to RecordPoint.
/// Aggregations represent a collection of records.
/// In the reference connector, aggregations are a folder that contains documents.
/// </summary>
/// <remarks>
/// Aggregations are passed into this service by the Content Sync and Content Reg services.
/// </remarks>
public static class Program
{
    public static void Main(string[] args)
    {
        CreateConnectorHostBuilder(args)
            .Build()
            .Run();
    }

    private static IHostBuilder CreateConnectorHostBuilder(string[] args)
    {
        var builder = HostBuilderHelper.CreateConnectorHostBuilder(args);
        
        // This service does not communicate with the content source,
        // so it does not require any custom code. 
        // All you need to implement the service is this DI code:
        builder.HostBuilder
            .UseR365AppSettingsConfiguration()
            .UseR365Integration()
            .UseAggregationSubmissionOperation();

        return builder.HostBuilder;
    }
}