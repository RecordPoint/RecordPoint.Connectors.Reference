using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.ContentManager;
using RecordPoint.Connectors.SDK.R365;

namespace RecordPoint.Connectors.Reference.AggregationSubmission;
// The aggregation submission service submits aggregrations, these represent a collection of records that are submitted to RecordPoint.
// For the reference connector aggregations would be a folder within a directory that contains documents.
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

        // The Aggregation Submission service does not communicate with the content source,
        // so it does not require any custom code. 
        // All you need to implement the service is these methods:
        builder.HostBuilder
            .UseR365AppSettingsConfiguration()
            .UseR365Integration()
            .UseAggregationSubmissionOperation();

        return builder.HostBuilder;
    }
}