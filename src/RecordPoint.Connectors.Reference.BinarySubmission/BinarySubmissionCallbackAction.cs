using RecordPoint.Connectors.SDK.Abstractions.ContentManager;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.BinarySubmission;

/// <inheritdoc cref="IBinarySubmissionCallbackAction"/>
public class BinarySubmissionCallbackAction : IBinarySubmissionCallbackAction
{
    /// <summary>
    /// Used to clean up any temporary resources which the connector created during
    /// binary submission. Most connectors don't need this.
    /// (The Ref Connector doesn't need it either - it's included only for demonstration purposes.)
    /// </summary>
    public Task ExecuteAsync(ConnectorConfigModel connectorConfiguration, BinaryMetaInfo binaryMetaInfo,
        SubmissionActionType submissionActionType, CancellationToken cancellationToken)
    {
        // Stub for demonstration purposes.
        //
        // Example use of this method:
        // Problem: I downloaded the binary to a local file system during BinaryRetrievalAction.
        //            Now that it's submitted, I need to delete the file.
        // Implementation steps:
        // - Store file path in binaryMetaInfo during BinaryRetrievalAction.ExecuteAsync
        // - Fetch file path from binaryMetaInfo in this method
        // - Use path to find and delete file
        return Task.CompletedTask;
    }
}