using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.RecordDisposal
//This service is responsible for responding to disposal requests from the RecordPoint platform.
//I.e. if a record is disposed on the platform, this service will handle the disposal of the record in the content source.
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

            //Setup Reference Connector Content Services
            builder.HostBuilder
                .UseRecordDisposalOperation<RecordDisposalAction>();

            return builder.HostBuilder;
        }
    }
}