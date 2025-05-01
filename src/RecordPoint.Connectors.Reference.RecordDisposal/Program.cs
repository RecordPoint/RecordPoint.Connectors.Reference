using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.RecordDisposal;

/// <summary>
/// This service is responsible for handling disposal requests from the RecordPoint platform
/// by disposing records in the content source.
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
            .UseRecordDisposalOperation<RecordDisposalAction>();

        return builder.HostBuilder;
    }
}