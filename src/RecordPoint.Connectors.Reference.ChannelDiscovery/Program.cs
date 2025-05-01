using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ChannelDiscovery;

/// <summary>
/// This service is responsible for discovering channels in the content source.
/// </summary>
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
            .UseChannelDiscoveryOperation<ChannelDiscoveryAction>();

        return builder.HostBuilder;
    }
}