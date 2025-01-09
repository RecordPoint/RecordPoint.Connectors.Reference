using System.Text.Json;
using RecordPoint.Connectors.SDK.Caching.Semaphore;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.Common;

/// <summary>
/// The channel extensions.
/// </summary>
public static class ChannelHelper
{
    /// <summary>
    /// Get full channel asynchronously.
    /// </summary>
    /// <remarks>
    /// The Channel object passed into the entry points in each Action class
    /// (e.g. ContentSynchronisationAction.ExecuteAsync) is a stub (MetaDataItems is not populated).
    /// Use this method to populate the MetaDataItems property.
    /// </remarks>
    public static async Task<Channel> GetFullChannelAsync(this IChannelManager channelManager, string connectorConfigurationId, string channelId, CancellationToken cancellationToken)
    {
        var channelModel = await channelManager.GetChannelAsync(connectorConfigurationId, channelId, cancellationToken)
            ?? throw new InvalidOperationException($"Could not find channel: {channelId}");
        return channelModel.ConvertToChannel();
    }

    /// <summary>
    /// Converts ChannelModel to Channel.
    /// </summary>
    /// <remarks>
    /// ChannelModel stores channel metadata as a JSON string.
    /// Channel stores channel metadata in a List of MetaDataItem.
    /// </remarks>
    public static Channel ConvertToChannel(this ChannelModel model)
    {
        var result = new Channel
        {
            ExternalId = model.ExternalId,
            Title = model.Title ?? "",
            MetaDataItems = new List<MetaDataItem>()
        };
        if (model.MetaData == null)
            return result;

        var metaData = JsonSerializer.Deserialize<List<MetaDataItem>>(model.MetaData);
        if (metaData == null)
            return result;

        result.MetaDataItems = metaData;
        return result;
    }

    public static bool IsIncluded(ConnectorConfigModel connector, string channelName)
    {
        var ingestionMode = connector.GetIngestionMode();
        var included = connector.GetListProperty<string>(MetadataNames.Included);
        var excluded = connector.GetListProperty<string>(MetadataNames.Excluded);

        if (ingestionMode == ConnectorConfigOptions.IngestionMode.Selected)
        {
            // Item should be excluded, unless it was called out in the Included list
            return included != null && included.Any(dir => dir.Equals(channelName));
        }

        // Item should be included, unless it was called out in the Excluded list
        return excluded == null || !excluded.Any(dir => dir.Equals(channelName));
    }
    /// <summary>
    /// Return a ChannelDiscoveryResult with a BackOff result type, when the channel discovery is throttled.
    /// This will signal to the SDK to retry the channel discovery after the specified delay.
    /// </summary>
    /// <param name="ex"></param>
    /// <param name="delay"></param>
    /// <returns></returns>
    public static ChannelDiscoveryResult ThrottledChannelDiscoveryResult(Exception ex, int delay)
    {
        return new ChannelDiscoveryResult
        {
            Exception = ex,
            ResultType = ChannelDiscoveryResultType.BackOff,
            SemaphoreLockType = SemaphoreLockType.Scoped,
            NextDelay = delay
        };
    }
}
