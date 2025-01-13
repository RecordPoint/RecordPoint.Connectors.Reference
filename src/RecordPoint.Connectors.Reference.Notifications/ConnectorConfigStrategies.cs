using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Connectors;
using RecordPoint.Connectors.SDK.Notifications;
using RecordPoint.Connectors.SDK.Notifications.Handlers;

namespace RecordPoint.Connectors.Reference.Notifications;

/// <summary>
/// Override of some notification handlers from the SDK
/// to work better with the Ref Connector.
/// </summary>
/// <remarks>
/// No other connector types will need any of the classes in this file.
/// </remarks>
public class RefConnectorConfigUpdatedHandler : ConnectorConfigUpdatedHandler
{
    private readonly IConnectorConfigurationManager _configManager;

    public RefConnectorConfigUpdatedHandler(IConnectorConfigurationManager connectorManager,
        IConnectorConfigurationManager configManager) : base(connectorManager)
    {
        _configManager = configManager;
    }

    public override async Task<NotificationOutcome> HandleNotificationAsync(ConnectorNotificationModel notification,
        CancellationToken cancellationToken)
    {
        // Custom properties for our configs don't exist in R365
        // Have to fetch the existing properties from the DB or they'll be overriden
        var newConnector = notification.ConnectorConfig;
        var existingConnector = await _configManager.GetConnectorAsync(newConnector.Id, cancellationToken);
        newConnector.Properties = existingConnector.Properties;
        return await base.HandleNotificationAsync(notification, cancellationToken);
    }
}

public class RefConnectorConfigCreatedHandler : ConnectorConfigCreatedHandler
{
    private readonly IConnectorConfigurationManager _configManager;

    public RefConnectorConfigCreatedHandler(IConnectorConfigurationManager connectorManager,
        IConnectorConfigurationManager configManager) : base(connectorManager)
    {
        _configManager = configManager;
    }

    public override async Task<NotificationOutcome> HandleNotificationAsync(ConnectorNotificationModel notification,
        CancellationToken cancellationToken)
    {
        // Custom properties for our configs don't exist in R365
        // Have to fetch the existing properties from the DB or they'll be overriden
        var newConnector = notification.ConnectorConfig;
        var existingConnector = await _configManager.GetConnectorAsync(newConnector.Id, cancellationToken);
        newConnector.Properties = existingConnector.Properties;
        return await base.HandleNotificationAsync(notification, cancellationToken);
    }
}