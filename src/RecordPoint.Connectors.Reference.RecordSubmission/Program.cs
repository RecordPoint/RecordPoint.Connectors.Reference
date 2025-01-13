using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.ContentManager;
using RecordPoint.Connectors.SDK.R365;

namespace RecordPoint.Connectors.Reference.RecordSubmission;
//This service handles the submission of the records to the RecordPoint platform. It processes the records put on the record submission queue by the Content Registration or Content Synchronisation services.
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
        // All you need to implement the service is these methods:
        builder.HostBuilder
            .UseR365AppSettingsConfiguration()
            .UseR365Integration()
            .UseRecordSubmissionOperation();

        return builder.HostBuilder;
    }
}