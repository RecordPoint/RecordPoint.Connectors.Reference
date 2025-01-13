using RecordPoint.Connectors.SDK.Caching;
using RecordPoint.Connectors.SDK.Client.Models;

namespace RecordPoint.Connectors.Reference.Common;

public class SemaphoreLockScopedKeyAction : ISemaphoreLockScopedKeyAction
{
    public Task<string> ExecuteAsync(ConnectorConfigModel connectorConfigModel, string workType, object? context,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(workType);
    }
}