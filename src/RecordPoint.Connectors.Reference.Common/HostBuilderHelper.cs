using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.SDK;
using System.Reflection;
using RecordPoint.Connectors.SDK.Caching.Semaphore;
using RecordPoint.Connectors.SDK.Connectors;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.Context;
using RecordPoint.Connectors.SDK.Databases.Sqlite;
using RecordPoint.Connectors.SDK.Time;
using RecordPoint.Connectors.SDK.Work;
using RecordPoint.Connectors.SDK.WorkQueue.RabbitMq;
using RecordPoint.Connectors.SDK.Observability.Console;
using RecordPoint.Connectors.SDK.Observability.Null;
using RecordPoint.Connectors.SDK.Toggles.Development.LocalJsonToggles;

namespace RecordPoint.Connectors.Reference.Common;

/// <summary>
/// Shared dependency injection for all services.
/// </summary>
/// <remarks>
/// The Ref Connector is hosted "on-prem", and has minimal security concerns
/// or reliance on outside infrastructure.
/// Cloud-hosted connectors will register different resources to the Ref Connector.
/// </remarks>
public class HostBuilderHelper
{
    public static (IHostBuilder HostBuilder, IConfigurationRoot Configuration) CreateConnectorHostBuilder(string[] args)
    {
        // Read settings from appsettings file
        // (Outside the Ref Connector, should read from environment variables or Key Vault - see docs)
        var settingsPath = Path.GetFullPath("../../../../../appsettings.json");
        var configurationBuilder = ConnectorConfigurationBuilder
            .CreateConfigurationBuilder(args, Assembly.GetExecutingAssembly())
            .AddJsonFile(settingsPath);

        var configuration = configurationBuilder.Build();

        var hostBuilder = Host.CreateDefaultBuilder(args)
            .UseConfiguration(configuration)
            .UseSystemContext("RecordPoint", "Reference Connector", "Reference")
            .UseSystemTime();

        // Setup SQLite database for storage
        // (For cloud-hosted connectors, we prefer to use Cosmos - see docs)
        hostBuilder
            .UseSqliteConnectorDatabase()
            .UseDatabaseConnectorConfigurationManager()
            .UseDatabaseChannelManager();
    
        // Setup telemetry
        // (Outside the Ref Connector, add UseAppInsightsTelemetryTracking() here.)
        hostBuilder
            .UseConsoleLogging();

        // Set feature toggle provider
        // (Outside the Ref Connector, should use the Launch Darkly provider if possible - see docs)
        //See featureToggles.json for an example of the toggles and the toggle names
        //The toggle Get methods in the SDK expect a name that contains the name of the connector and the name of the toggle
        //The path to the json will also have to be provided in the appsettings.json
        hostBuilder.UseLocalFileToggleProvider();
        
        hostBuilder.UseInMemorySemaphoreLock<SemaphoreLockScopedKeyAction>();

        // Setup work state management
        hostBuilder
            .UseWorkManager()
            .UseWorkStateManager<DatabaseManagedWorkStatusManager>();

        // Setup work queues (including dead-letter queues)
        // (For cloud-hosted connectors, we prefer to use Azure Service Bus - see docs)
        hostBuilder
            .UseRabbitMqWorkQueue()
            .UseRabbitMqDeadLetterQueueService();

        // Custom settings for the Reference Connector
        // (Should not be required for other connectors)
        hostBuilder.UseNullTelemetryTracking();

        return (hostBuilder, configuration);
    }
}