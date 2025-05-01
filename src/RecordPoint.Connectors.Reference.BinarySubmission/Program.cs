using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.ContentManager;
using RecordPoint.Connectors.SDK.R365;

namespace RecordPoint.Connectors.Reference.BinarySubmission;

/// <summary>
/// This service is responsible for submitting the actual binary content of the records
/// to the RecordPoint platform.
/// </summary>
/// <remarks>
/// Binary metadata is passed into this service by the Content Sync and Content Reg services.
/// This metadata is then used to fetch the binaries and pass onto RecordPoint.
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

        // If you don't need post-submission cleanup:
        // - use UseBinarySubmissionOperation<BinaryRetrievalAction>()
        //   (instead of UseBinarySubmissionOperation<BinaryRetrievalAction, BinarySubmissionCallbackAction>)
        // - do not implement BinarySubmissionCallbackAction
        // Refer BinarySubmissionCallbackAction for more details.
        builder.HostBuilder
            .UseR365AppSettingsConfiguration()
            .UseR365Integration()
            .UseBinarySubmissionOperation<BinaryRetrievalAction, BinarySubmissionCallbackAction>();

        return builder.HostBuilder;
    }
}