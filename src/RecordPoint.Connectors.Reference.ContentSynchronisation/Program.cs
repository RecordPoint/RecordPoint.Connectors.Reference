using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ContentSynchronisation;
// This service queues up aggregations and records to be submitted to the RecordPoint platform and it runs periodically checking for new content to be submitted to the RecordPoint platform.
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
            .UseContentSynchronisationOperation<ContentSynchronisationAction>();

        return builder.HostBuilder;
    }
}