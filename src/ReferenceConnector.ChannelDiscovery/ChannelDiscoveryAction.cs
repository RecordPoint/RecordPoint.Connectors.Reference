using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Caching.Semaphore;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ChannelDiscovery;

/// <inheritdoc cref="IChannelDiscoveryAction"/>
/// <remarks>
/// The results of this service are passed to ContentRegistration and ContentSynchronisation,
/// which use the Channels to start syncing content to RecordPoint.
/// 
/// A channel is an arbitrary container from the content source that is used as a basis for queries
/// in ingestion-related SDK services. It should not be submitted to RecordPoint itself.
/// </remarks>
public class ChannelDiscoveryAction : IChannelDiscoveryAction
{
    private readonly IChannelManager _channelManager;

    public ChannelDiscoveryAction(IChannelManager channelManager)
    {
        _channelManager = channelManager;
    }

    public async Task<ChannelDiscoveryResult> ExecuteAsync(ConnectorConfigModel connectorConfiguration,
        CancellationToken cancellationToken)
    {
        try
        {
            return await FindChannels(connectorConfiguration, cancellationToken);
        }
        catch(Exception ex)
        {
            return new ChannelDiscoveryResult
            {
                Exception = ex,
                ResultType = ChannelDiscoveryResultType.Failed,
            };
        }
    }

    public async Task<ChannelDiscoveryResult> FindChannels(ConnectorConfigModel connectorConfiguration,
        CancellationToken cancellationToken)
    {
        var connectorDir = connectorConfiguration.GetPropertyOrDefault(MetadataNames.Directory);
        if (string.IsNullOrEmpty(connectorDir))
        {
            throw new InvalidOperationException($"Connector [{connectorConfiguration.Id}] does not contain Directory property");
        }

        var foundChannels = new List<Channel>();
        var knownChannels = await _channelManager
            .GetChannelsAsync(connectorConfiguration.Id, cancellationToken);
        var auditEvents = new List<AuditEvent>();
        IEnumerable<string> subDirPaths;

        //Since the EnumerateDirectories method deals with getting data from the content source, this would be where throttling might occur.
        try
        {
            subDirPaths = Directory.EnumerateDirectories(connectorDir);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException)
        {
            // Retry after 60 seconds, for other connectors exceptions might themselves provide a delay or retry after value
            var delay = 60;
            return ChannelHelper.ThrottledChannelDiscoveryResult(ex, delay);
        }

        foreach (var subDirPath in subDirPaths)
        {
            var pathSegments = subDirPath.Split('\\');
            var title = pathSegments.Last();

            //We use title here because we are dealing with Filenames for other connectors an ExternalID might be more appropriate
            if (!ChannelHelper.IsIncluded(connectorConfiguration, title)) continue;

            var channel = new Channel
            {
                Title = title,
                MetaDataItems = GetMetadata(subDirPath),
                ExternalId = GetExternalId(title, knownChannels)
            };
            foundChannels.Add(channel);
        }

        var newChannels = foundChannels.ExceptBy(knownChannels.Select(channel => channel.ExternalId),
            channel => channel.ExternalId).ToList();

        return new ChannelDiscoveryResult
        {
            Reason = "Completed",
            ResultType = ChannelDiscoveryResultType.Complete,

            // All new and updated channels for this connector config.
            // (The SDK will update these in storage)
            // (Passing in ALL existing channels - as done here - won't break anything)
            Channels = foundChannels,

            // All new channels for this connector config.
            // (The SDK will create Content Sync & Registration operations for these)
            NewChannelRegistrations = newChannels,

            // Channel Discovery can also submit Audit Events if needed.
            AuditEvents = auditEvents
        };
    }

    private string GetExternalId(string channelName, List<ChannelModel> knownChannels)
    {
        // A metadata from the content source that uniquely identifies the item.
        // Should remain constant through the item's lifetime.
        // (For the Ref Connector, we have to use a 'fake' ID, as there is no suitable metadata available. 
        // This means we can't tell when a file has been renamed or moved.
        // Avoid doing this outside the Ref Connector.)
        var existingChannel = knownChannels.FirstOrDefault(c => c.Title != null && c.Title.Equals(channelName));
        return existingChannel?.ExternalId ?? Guid.NewGuid().ToString();
    }

    private List<MetaDataItem> GetMetadata(string subDirPath)
    {
        var subDir = new DirectoryInfo(subDirPath);

        // Channels (unlike Records and Aggregations) are not submitted to RecordPoint.
        // Metadata should only be added if needed by later services.
        var metadataItems = new List<MetaDataItem>
        {
            new() {
                Name = MetadataNames.Path,
                Type = RecordPointDataTypes.String,
                Value = subDir.FullName
            }
        };

        return metadataItems;
    }
}