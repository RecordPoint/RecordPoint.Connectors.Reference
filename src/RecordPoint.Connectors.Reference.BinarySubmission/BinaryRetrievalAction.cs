using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.Reference.Common.Abstractions;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.BinarySubmission;

/// <inheritdoc cref="IBinaryRetrievalAction"/>
public class BinaryRetrievalAction : IBinaryRetrievalAction
{
    private readonly IFileSystemClient _fileSystemClient;

    public BinaryRetrievalAction(IFileSystemClient fileSystemClient)
    {
        _fileSystemClient = fileSystemClient;
    }

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

        FileStream? stream;
        try
        {
            // Make sure not to make the stream disposable (e.g. via 'using').
            // The Connectors.SDK will handle stream disposal.
            stream = _fileSystemClient.GetBinaryStream(path);
        }
        catch (Exception ex) when (ContentSourceHelper.IsThrottlingException(ex))
        {
            var delay = ContentSourceHelper.GetBackoffTimeSeconds(ex);
            return new BinaryRetrievalResult
            {
                ResultType = BinaryRetrievalResultType.BackOff,
                NextDelay = delay
            };
        }

        if(stream == null || stream.Length == 0)
        {
            // Make sure to return Abandoned for empty or missing binaries
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