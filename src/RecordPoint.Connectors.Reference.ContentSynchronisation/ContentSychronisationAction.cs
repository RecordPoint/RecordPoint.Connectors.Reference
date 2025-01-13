using System.Globalization;
using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ContentSynchronisation;

/// <inheritdoc cref="IContentSynchronisationAction"/>
/// <remarks>
/// Initiates most ingestion into RecordPoint.
/// The SDK automatically passes data from this service into other services
/// (e.g. RecordSubmission, AggregationSubmission).
/// </remarks>
public class ContentSynchronisationAction : IContentSynchronisationAction
{
    private readonly IChannelManager _channelManager;

    public ContentSynchronisationAction(IChannelManager channelManager)
    {
        _channelManager = channelManager;
    }

    public async Task<ContentResult> BeginAsync(ConnectorConfigModel connectorConfiguration, Channel channel,
        DateTimeOffset startDate, CancellationToken cancellationToken)
    {
        try
        {
            // In other connectors, this (usually) isn't needed. Just pass an empty cursor to PollChanges.
            //
            // In an ideal situation, the content source will provide 2 APIs:
            // - 1 for reading the current state of data
            // - 1 for monitoring changes 
            // We'd use the first for content reg, and the second for content sync.
            //
            // For the Ref Connector, we only have a "current state of data" API.
            // So we need to use the date content reg & sync started as an initial cursor,
            // so the data synced by content reg and content sync don't overlap.
            var startTime = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            var result = await PollChanges(connectorConfiguration, channel, startTime, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            return ContentHelper.FailedResult(string.Empty, ex);
        }
    }

    public async Task<ContentResult> ContinueAsync(ConnectorConfigModel connectorConfiguration, Channel channel,
        string cursor, CancellationToken cancellationToken)
    {
        try
        {
            return await PollChanges(connectorConfiguration, channel, cursor, cancellationToken);
        }
        catch (Exception ex)
        {
            return ContentHelper.FailedResult(cursor, ex);
        }
    }

    private async Task<ContentResult> PollChanges(ConnectorConfigModel connectorConfiguration, Channel channel,
        string cursor, CancellationToken cancellationToken)
    {
        channel = await _channelManager.GetFullChannelAsync(connectorConfiguration.Id,
            channel.ExternalId ?? string.Empty, cancellationToken);

        try
        {
            _ = DateTime.TryParse(cursor, CultureInfo.InvariantCulture, out var lastPolledTime);
            var startTime = DateTime.UtcNow;
            var channelPath = channel.MetaDataItems.First(x => x.Name.Equals(MetadataNames.Path));
            var channelDir = new DirectoryInfo(channelPath.Value);
            var auditEvents = new List<AuditEvent>();

            var channelValidationResult = ValidateChannel(connectorConfiguration, channel, channelDir);
            if (channelValidationResult != null)
            {
                return channelValidationResult;
            }

            var aggregations = channelDir.EnumerateDirectories();
            var sdkAggregations = new List<Aggregation>();
            var sdkRecords = new List<Record>();

            foreach (var aggregation in aggregations)
            {
                var (sdkAggregation, shouldNotSubmitAggVersion) =
                    SdkConverter.GetSdkAggregation(aggregation, channel, lastPolledTime);

                var childRecords = aggregation.EnumerateFiles();

                var (sdkChildRecords, recordAuditEvents) =
                    SdkConverter.GetSdkRecords(childRecords, sdkAggregation, lastPolledTime);

                sdkRecords.AddRange(sdkChildRecords);
                auditEvents.AddRange(recordAuditEvents);
                if (!shouldNotSubmitAggVersion)
                    sdkAggregations.Add(sdkAggregation);
            }

            var contentResult = new ContentResult
            {
                //The SDK will submit records,aggregations audit events to RecordPoint
                Records = sdkRecords,
                Aggregations = sdkAggregations,

                // The cursor keeps track of where in the content source we're 'up to'.
                // Ideally we'd use something from the content source, e.g. a page token.
                // (Outside of the Reference Connector, avoid DateTime cursors.)
                Cursor = startTime.ToString(CultureInfo.InvariantCulture),

                // If you know there are more objects in the content source remaining to be synced,
                // use ContentResultType.Incomplete instead.
                // The SDK will then schedule this job to run again ASAP.
                ResultType = ContentResultType.Complete,
                AuditEvents = auditEvents
            };

            return contentResult;
        }
        catch (Exception ex) when(ex is IOException || ex is TimeoutException)
        {
            //These exceptions might result from throttling, this is the assumption which we will make for this connector.
            //The following is an example of how to handle such situations.
            var delay = 30;
            return ContentHelper.ThrottledContentResult(ex, delay);
        }
    }

    public Task StopAsync(ConnectorConfigModel connectorConfiguration, Channel channel, string cursor,
        CancellationToken cancellationToken)
    {
        // Most connectors won't need to implement this. Refer method summary.
        return Task.CompletedTask;
    }

    private ContentResult? ValidateChannel(ConnectorConfigModel config, Channel channel, DirectoryInfo channelDir)
    {
        // Content Sync is responsible for checking that the Channel is valid.
        // (No other service needs to do this.)
        if (!channelDir.Exists)
        {
            return ContentHelper.AbandonResult("Directory no longer exists");
        }

        if (!ChannelHelper.IsIncluded(config, channel.Title))
        {
            return ContentHelper.AbandonResult("Channel now excluded by filters");

        }

        return null;
    }
}