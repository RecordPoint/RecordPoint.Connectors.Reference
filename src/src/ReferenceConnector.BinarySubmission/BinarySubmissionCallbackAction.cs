using RecordPoint.Connectors.SDK.Abstractions.ContentManager;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.BinarySubmission;

/// <inheritdoc cref="IBinarySubmissionCallbackAction"/>
public class BinarySubmissionCallbackAction : IBinarySubmissionCallbackAction
{
    /// <summary>
    /// Used to clean up any temporary resources a connector may create during
    /// binary submission. Most connectors don't need this.
    /// (The Ref connector doesn't need it either - it's included only for demonstration purposes.)
    /// </summary>
    public Task ExecuteAsync(ConnectorConfigModel connectorConfiguration, BinaryMetaInfo binaryMetaInfo,
        SubmissionActionType submissionActionType, CancellationToken cancellationToken)
    {
        // Stub for demonstration purposes.
        return Task.CompletedTask;
    }
}