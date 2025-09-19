using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.ContentManager;
using RecordPoint.Connectors.SDK.R365;

namespace RecordPoint.Connectors.Reference.RecordSubmission;

/// <summary>
/// This service handles the submission of records to the RecordPoint platform.
/// In the reference connector, records are documents in a file system.
/// </summary>
/// <remarks>
/// Records are passed into this service by the Content Sync and Content Reg services.
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

        // The Record Submission service does not communicate with the content source,
        // so it does not require any custom code. 
        // All you need to implement the service is this dependency injection code:
        builder.HostBuilder
            .UseR365AppSettingsConfiguration()
            .UseR365Integration()
            .UseRecordSubmissionOperation();

        return builder.HostBuilder;
    }
}