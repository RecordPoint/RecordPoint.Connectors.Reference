using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.ContentManager;
using RecordPoint.Connectors.SDK.R365;

namespace RecordPoint.Connectors.Reference.BinarySubmission;
//This service is responsible for submitting the actual binary content of the records to the RecordPoint platform. The classes in this 
//project have further detailed comments and descriptions of what a service like this would entail.
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

        // If you don't need any post-submission cleanup:
        // - use UseBinarySubmissionOperation<BinaryRetrievalAction>() here
        // - skip implementing BinarySubmissionCallbackAction
        builder.HostBuilder
            .UseR365AppSettingsConfiguration()
            .UseR365Integration()
            .UseBinarySubmissionOperation<BinaryRetrievalAction, BinarySubmissionCallbackAction>();

        return builder.HostBuilder;
    }
}