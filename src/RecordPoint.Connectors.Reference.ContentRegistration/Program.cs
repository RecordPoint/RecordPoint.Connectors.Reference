using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ContentRegistration;
// This service queues up aggregations and records to be submitted to the RecordPoint platform. It is similar to the ContentSyncronisation service
// in what it does,however it is only run once and deals with historical content exclusively in the discovered channels.
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

        builder.HostBuilder
            .UseContentRegistrationOperation<ContentRegistrationAction>();

        return builder.HostBuilder;
    }
}
