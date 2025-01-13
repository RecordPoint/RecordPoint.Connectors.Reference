using System.Text.Json;
using RecordPoint.Connectors.SDK.Client.Models;

namespace RecordPoint.Connectors.Reference.Common;

public static class ConnectorConfigExtensions
{
    public static ConnectorConfigOptions.IngestionMode GetIngestionMode(this ConnectorConfigModel connector)
    {
        var ingestionModeString = connector.GetPropertyOrDefault(MetadataNames.DefaultIngestionMode);
        if (ingestionModeString == null)
        {
            throw new InvalidOperationException($"Connector [{connector.Id}] is malformed - missing DefaultIngestionMode property.");
        }

        var isValid = Enum.TryParse<ConnectorConfigOptions.IngestionMode>(ingestionModeString, out var ingestionMode);
        if (!isValid)
        {
            throw new InvalidOperationException($"Connector [{connector.Id}] is malformed - invalid DefaultIngestionMode property.");
        }

        return ingestionMode;
    }

    public static List<T>? GetListProperty<T>(this ConnectorConfigModel connector, string propertyName)
    {
        var listString = connector.GetPropertyOrDefault(propertyName);
        return listString == null ? null : JsonSerializer.Deserialize<List<T>>(listString);
    }
}