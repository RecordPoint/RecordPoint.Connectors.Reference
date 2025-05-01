using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Configuration;
using RecordPoint.Connectors.SDK.Notifications;
using RecordPoint.Connectors.SDK.Notifications.Handlers;

namespace RecordPoint.Connectors.Reference.Notifications;

/// <summary>
/// The Notification Service is responsible for receiving notifications about actions
/// that users perform in the RecordPoint platform which affect the connector
/// (e.g. created or edited connector configs, disposed records).
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

        // The Reference Connector uses Pull/Polling notifications.
        // Other connectors should use Webhook notifications (unless otherwise indicated).
        // To do this, use UseWebhookNotifications()
        // instead of UsePolledNotifications()
        builder.HostBuilder
            .UseR365AppSettingsConfiguration()
            .UsePolledNotifications<ContentRegistrationRequestAction>();

        AddReferenceConnectorNotificationServices(builder.HostBuilder);

        return builder.HostBuilder;
    }

    /// <summary>
    /// Contains DI that is NOT REQUIRED outside the ReferenceConnector.
    /// </summary>
    private static void AddReferenceConnectorNotificationServices(IHostBuilder builder)
    {
        // Override the notification DI stuff that the SDK registered.
        // See ConnectorConfigCreatorService for an explanation of why.
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