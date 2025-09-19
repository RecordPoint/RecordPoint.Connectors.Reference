using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.ContentManager;
using RecordPoint.Connectors.SDK.R365;

namespace RecordPoint.Connectors.Reference.AuditEventSubmission;

/// <summary>
/// The AuditEventSubmission service is responsible for submitting Audit Events
/// to the RecordPoint platform.
/// Data management platforms often have audit events that users wish to track and view.
/// </summary>
/// <remarks>
/// Audit events are passed into this service by the Content Sync, Content Reg and Channel Discovery services.
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

        // This service does not communicate with the content source,
        // so it does not require any custom code.
        // All you need to implement the service is this dependency injection code:
        builder.HostBuilder
            .UseR365AppSettingsConfiguration()
            .UseR365Integration()
            .UseAuditEventSubmissionOperation();

        return builder.HostBuilder;
    }
}