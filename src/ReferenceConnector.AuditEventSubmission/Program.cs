using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.ContentManager;
using RecordPoint.Connectors.SDK.R365;

namespace RecordPoint.Connectors.Reference.AuditEventSubmission
//The AuditEventSubmission service is responsible for submitting Audit Events to the RecordPoint platform. Data management platforms often have Audit events that users wish to track
//and view.
{
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

            //There are examples of how to submit Audit Events from the Content Sync, Content Registration and Channel Discovery services.
            //The SDK makes use of Results for the services and allows for the submission of Audit Events from the services. As this is a property
            //of the results class for instance the ContentResult class or the ChannelDiscoveryResult class.
            builder.HostBuilder
                .UseR365AppSettingsConfiguration()
                .UseR365Integration()
                .UseAuditEventSubmissionOperation();

            return builder.HostBuilder;
        }
    }
}