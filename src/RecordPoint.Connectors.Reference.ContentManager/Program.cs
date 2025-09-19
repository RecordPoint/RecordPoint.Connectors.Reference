using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ContentManager;

/// <summary>
/// The Content Manager service kicks off internal operations
/// for new connector configurations (e.g. Channel Discovery, Content Registration).
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

        // The Content Manager service does not communicate with the content source,
        // so it does not require any custom code. 
        // All you need to implement the service is this dependency injection code:
        builder.HostBuilder.UseContentManagerService();

        return builder.HostBuilder;
    }
}
