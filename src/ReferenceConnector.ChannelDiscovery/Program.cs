using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ChannelDiscovery;
// This service is resposible for discovering channels in the content source. The ChannelDiscoveryAction class is where you will implement the logic to discover channels for your content source
//and there are further detailed comments abouut what a service like this would entail.
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