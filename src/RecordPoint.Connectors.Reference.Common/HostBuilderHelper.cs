using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RecordPoint.Connectors.SDK;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RecordPoint.Connectors.Reference.Common.Abstractions;
using RecordPoint.Connectors.SDK.Caching.Semaphore;
using RecordPoint.Connectors.SDK.Connectors;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.Context;
using RecordPoint.Connectors.SDK.Databases.Cosmos;
using RecordPoint.Connectors.SDK.Databases.Sqlite;
using RecordPoint.Connectors.SDK.Health;
using RecordPoint.Connectors.SDK.Observability.AppInsights;
using RecordPoint.Connectors.SDK.Time;
using RecordPoint.Connectors.SDK.Work;
using RecordPoint.Connectors.SDK.WorkQueue.RabbitMq;
using RecordPoint.Connectors.SDK.Observability.Console;
using RecordPoint.Connectors.SDK.Observability.Null;
using RecordPoint.Connectors.SDK.Status;
using RecordPoint.Connectors.SDK.Toggles.Development.LocalJsonToggles;
using RecordPoint.Services.Common.Configuration;
using RecordPoint.Connectors.SDK.Toggles.LaunchDarkly;
using RecordPoint.Connectors.SDK.WebHost;
using RecordPoint.Connectors.SDK.WorkQueue.AzureServiceBus;

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
    /// <summary>
    /// Sets up dependency injection required for on-prem connectors.
    /// </summary>
    /// <remarks>
    /// Refer CreateCloudConnectorHostBuilder() below for how cloud connectors are set up.
    /// 
    /// The on-prem set of resources is:
    /// - Queueing: RabbitMq
    /// - Storage: Sqlite
    /// - Logging: Console
    /// - Feature toggles: N/A
    /// - Settings: local appsettings.json file
    /// </remarks>
    public static (IHostBuilder HostBuilder, IConfigurationRoot Configuration) CreateConnectorHostBuilder(string[] args)
    {
        // Read settings from appsettings file
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
        hostBuilder
            .UseSqliteConnectorDatabase()
            .UseDatabaseConnectorConfigurationManager()
            .UseDatabaseChannelManager();
    
        // Setup telemetry
        hostBuilder
            .UseConsoleLogging();

        // Set feature toggle provider
        // See featureToggles.json for an example of the toggles and the toggle names
        // The toggle Get methods in the SDK expect a name that contains the name of the connector and the name of the toggle
        // The path to the json will also have to be provided in the appsettings.json
        hostBuilder.UseLocalFileToggleProvider();
        
        //Semaphore Locking for throttling from the content source
        hostBuilder
            .UseInMemorySemaphoreLock<SemaphoreLockScopedKeyAction>()
            .ConfigureServices(services => services.AddMemoryCache());

        // Setup work state management
        hostBuilder
            .UseWorkManager()
            .UseWorkStateManager<DatabaseManagedWorkStatusManager>();

        // Setup work queues (including dead-letter queues)
        hostBuilder
            .UseRabbitMqWorkQueue()
            .UseRabbitMqDeadLetterQueueService();

        // Custom settings for the Reference Connector
        // (NOT REQUIRED for other connectors)
        hostBuilder.UseNullTelemetryTracking();
        hostBuilder.ConfigureServices(services =>
        {
            services
                .AddSingleton<IFileSystemClient, FileSystemClient>();
        });

        return (hostBuilder, configuration);
    }

    /// <summary>
    /// Sample function showing how dependency injection would be set up in a cloud-based connector.
    /// </summary>
    /// <remarks>
    /// The cloud set of resources is:
    /// - Queueing: Azure Service Bus
    /// - Storage: Cosmos
    /// - Logging: Application Insights
    /// - Feature toggles: LaunchDarkly
    /// - Settings: Key Vault
    /// </remarks>
    public static (IHostBuilder HostBuilder, IConfigurationRoot Configuration) CreateCloudConnectorHostBuilder(string[] args)
    {
        var configuration = ConnectorConfigurationBuilder
            .CreateConfigurationBuilder(args, Assembly.GetExecutingAssembly())
            .UseAzureKeyVaultConfigurationProvider("Reference Connector")
            .Build();

        var hostBuilder = Host.CreateDefaultBuilder(args)
            .UseConfiguration(configuration)
            .UseSystemContext("RecordPoint", "Reference Connector", "Reference")
            .UseSystemTime();

        //Setup Connector Database
        hostBuilder
            .UseCosmosDbConnectorDatabase()
            .UseDatabaseConnectorConfigurationManager()
            .UseDatabaseAggregationManager()
            .UseDatabaseChannelManager();
    
        // Setup telemetry
        hostBuilder
            .UseConsoleLogging()
            .UseAppInsightsTelemetryTracking();

        // Set feature toggle provider
        hostBuilder.UseLaunchDarklyToggles();
        
        //Semaphore Locking for throttling from the content source
        hostBuilder
            .UseInMemorySemaphoreLock<SemaphoreLockScopedKeyAction>()
            .ConfigureServices(services => services.AddMemoryCache());

        //Setup Status Manager & Health Checker
        hostBuilder
            .UseWebHost(configuration)
            .UseStatusManager()
            .UseHealthChecker();

        // Setup work state management
        hostBuilder
            .UseWorkManager()
            .UseWorkStateManager<DatabaseManagedWorkStatusManager>();

        // Setup work queues (including dead-letter queues)
        hostBuilder
            .UseASBWorkQueue()
            .UseASBDeadLetterQueueService();
        
        return (hostBuilder, configuration);
    }
}