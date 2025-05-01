using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Connectors;

namespace RecordPoint.Connectors.Reference.Notifications;

/// <summary>
/// Adds connector configurations listed in the user's appsettings file to the connector config database.
/// </summary>
/// <remarks>
/// NOT NEEDED OUTSIDE REFERENCE CONNECTOR.
/// (The Ref Connector needs this because it has no way to receive notifications for a config
/// until it knows it exists.)
/// </remarks>
public class ConnectorConfigCreatorService : IHostedService
{
    private readonly List<ConnectorConfigOptions> _configs;
    private readonly IConnectorConfigurationManager _configManager;

    public ConnectorConfigCreatorService(IOptions<List<ConnectorConfigOptions>> configOptions,
        IConnectorConfigurationManager configManager)
    {
        _configs = configOptions.Value;
        _configManager = configManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var dbConfigs = await _configManager.GetAllConnectorConfigurationsAsync(cancellationToken);

        foreach (var config in _configs)
        {
            var existsInDb = dbConfigs.Exists(c => c.ConnectorId.Equals(config.ConnectorId));

            // Once the connector is in the DB, the Notification service will monitor for changes.
            // No need to edit details.
            if (existsInDb) continue;

            var configForDb = new ConnectorConfigModel
            {
                // Any dummy values will be overriden by the 'real' value
                // once we get our first notification for this connector from R365
                Id = config.ConnectorId,
                TenantId = config.TenantId,
                ConnectorTypeId = "RefConnectorId", // Dummy value
                Status = "Disabled", // Dummy value
                DisplayName = config.ConnectorId,
                TenantDomainName = config.TenantDomainName,

                Properties = new List<MetaDataModel>
                {
                    new()
                    {
                        Name = MetadataNames.Directory,
                        Type = RecordPointDataTypes.String,
                        Value = config.Directory
                    },
                    new()
                    {
                        Name = MetadataNames.DefaultIngestionMode,
                        Type = RecordPointDataTypes.String,
                        Value = config.DefaultIngestionMode.ToString()
                    },
                    new()
                    {
                        Name = MetadataNames.Excluded,
                        Type = RecordPointDataTypes.String,
                        Value = JsonSerializer.Serialize(config.Excluded) 
                    },
                    new()
                    {
                        Name = MetadataNames.Included,
                        Type = RecordPointDataTypes.String,
                        Value = JsonSerializer.Serialize(config.Included)
                    }, 
                    new()
                    {
                        Name = MetadataNames.ContentRegistrationMode,
                        Type = RecordPointDataTypes.String,
                        Value = config.ContentRegistrationMode.ToString()
                        }
                }
            };

            await _configManager.SetConnectorAsync(configForDb, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}