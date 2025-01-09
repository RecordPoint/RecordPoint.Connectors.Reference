using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ContentManager;
// Kicks off operations (e.g. Channel Discovery) for new connector configurations
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
        // The Content Manager service does not communicate with the content source,
        // so it does not require any custom code. 
        // All you need to implement the service is this method:
        builder.HostBuilder.UseContentManagerService();

        return builder.HostBuilder;
    }
}
