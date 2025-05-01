using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ContentRegistration;

/// <summary>
/// This service queries the content source to find data that should be submitted to the RecordPoint platform.
/// It is similar to the ContentSynchronisation service.
/// Unlike Content Sync, it runs only once, submitting ONLY historical data (for the channels requested).
/// </summary>
/// <remarks>
/// This service does not submit data to RecordPoint.
/// It sends data to the Submission services (e.g. AggregationSubmission), which then submit the data.
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

        builder.HostBuilder
            .UseContentRegistrationOperation<ContentRegistrationAction>();

        return builder.HostBuilder;
    }
}
