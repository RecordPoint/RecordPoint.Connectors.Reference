using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.Notifications;
using RecordPoint.Connectors.SDK.Notifications.Handlers;

namespace RecordPoint.Connectors.Reference.Notifications;
//This is the service responsible for receiving notifications about operations that occur in the RecordPoint platform
//(e.g. created or edited connectors, disposed records.)'.
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

        // Use Pull Notifications
        // Most connectors would use Webhook notifications instead -
        // use UseWebhookNotifications() instead of UsePolledNotifications().
        builder.HostBuilder
            .UseR365AppSettingsConfiguration()
            //This starts sends off a Content Registration Request when a connector is updated
            .UsePolledNotifications<ContentRegistrationRequestAction>();

        AddReferenceConnectorNotificationServices(builder.HostBuilder);

        return builder.HostBuilder;
    }

    /// <summary>
    /// Contains DI that is not required outside the ReferenceConnector.
    /// (See ConnectorConfigCreatorService for more info.)
    /// </summary>
    private static void AddReferenceConnectorNotificationServices(IHostBuilder builder)
    {
        builder.ConfigureServices((hostContext, services) =>
        {
            services.Configure<List<ConnectorConfigOptions>>(
                hostContext.Configuration.GetSection(ConnectorConfigOptions.SECTION_NAME));
            services.AddHostedService<ConnectorConfigCreatorService>();

            var existingUpdateStrategy = services.First(d => d.ImplementationType == typeof(ConnectorConfigUpdatedHandler));
            var existingCreateStrategy = services.First(d => d.ImplementationType == typeof(ConnectorConfigCreatedHandler));
            services.Remove(existingUpdateStrategy);
            services.Remove(existingCreateStrategy);

            var updateStrategy = new ServiceDescriptor(typeof(INotificationStrategy),
                typeof(RefConnectorConfigUpdatedHandler),
                ServiceLifetime.Singleton);
            var createStrategy = new ServiceDescriptor(typeof(INotificationStrategy),
                typeof(RefConnectorConfigCreatedHandler),
                ServiceLifetime.Singleton);

            services.Add(updateStrategy);
            services.Add(createStrategy);
        });
    }
}