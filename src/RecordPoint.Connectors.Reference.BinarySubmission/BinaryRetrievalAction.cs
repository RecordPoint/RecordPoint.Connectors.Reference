using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.BinarySubmission;

/// <inheritdoc cref="IBinaryRetrievalAction"/>
public class BinaryRetrievalAction : IBinaryRetrievalAction
{
    /// <summary>
    /// Fetches a stream of the binary from the content source.
    /// The Connectors.SDK will handle submission of the binary to Records365
    /// using the returned stream and metadata.
    /// </summary>
    /// <remarks>
    /// binaryMetaInfo contains the metadata of any binaries that were returned
    /// as part of Records (in Content Registration and Content Synchronisation).
    /// </remarks>
    public async Task<BinaryRetrievalResult> ExecuteAsync(ConnectorConfigModel connectorConfiguration,
        BinaryMetaInfo binaryMetaInfo, CancellationToken cancellationToken)
    {
        try
        {
            return await FetchBinary(connectorConfiguration, binaryMetaInfo, cancellationToken);
        }
        catch(Exception ex)
        {
            return new BinaryRetrievalResult
            {
                Exception = ex,
                ResultType = BinaryRetrievalResultType.Failed,
            }; 
        }
    }

    private async Task<BinaryRetrievalResult> FetchBinary(ConnectorConfigModel connectorConfiguration,
        BinaryMetaInfo binaryMetaInfo, CancellationToken cancellationToken)
    {
        // You can use ContentToken or a value in MetaDataItems (set in Content Reg / Sync)
        // to pass through the info required to fetch the binary.
        var path = binaryMetaInfo.ContentToken;

        if(!File.Exists(path))
        {
            return new BinaryRetrievalResult
            {
                ResultType = BinaryRetrievalResultType.Abandoned
            };
        }

        // Make sure not to make the stream disposable (e.g. via 'using').
        // The Connectors.SDK will handle stream disposal.
        var stream = File.OpenRead(path);

        // Make sure to return BinaryRetrievalResultType.ZeroBinary
        // for empty binaries
        if (stream.Length == 0)
        {
            await stream.DisposeAsync();
            return new BinaryRetrievalResult
            {
                ResultType = BinaryRetrievalResultType.Abandoned
            };
        }

        return new BinaryRetrievalResult
        {
            Stream = stream,
            ResultType = BinaryRetrievalResultType.Complete
        };
    }
}