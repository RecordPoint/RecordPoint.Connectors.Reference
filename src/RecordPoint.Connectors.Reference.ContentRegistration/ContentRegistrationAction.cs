using RecordPoint.Connectors.Reference.Common;
using RecordPoint.Connectors.Reference.Common.Abstractions;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.ContentRegistration;

/// <summary>
/// Performs a one-time sync of historical data (i.e. data that existed in the content source
/// before the RecordPoint connector config was enabled), per channel.
/// </summary>
/// <inheritdoc cref="IContentRegistrationAction"/>
public class ContentRegistrationAction : IContentRegistrationAction
{
    private readonly IChannelManager _channelManager;
    private readonly IFileSystemClient _fileSystemClient;

    public ContentRegistrationAction(IChannelManager channelManager, IFileSystemClient fileSystemClient)
    {
        _channelManager = channelManager;
        _fileSystemClient = fileSystemClient;
    }

    /// <summary>
    /// Called for the first execution of Content Reg for this channel.
    /// </summary>
    /// <remarks>
    /// If BeginAsync() does not discover all the content from this channel,
    /// Content Reg will run again via ContinueAsync().
    /// </remarks>
    /// <param name="connectorConfiguration"></param>
    /// <param name="channel">Stripped down model of channel without any custom metadata.</param>
    /// <param name="context">
    /// Information provided on the user's content registration request,
    /// e.g. only items from the last 3 months should be registered. Possible values are defined per connector type.</param>
    /// <param name="cancellationToken"></param>
    public async Task<ContentResult> BeginAsync(ConnectorConfigModel connectorConfiguration, Channel channel,
        IDictionary<string, string> context, CancellationToken cancellationToken)
    {
        try
        {
            if (connectorConfiguration.GetPropertyOrDefault(MetadataNames.ContentRegistrationMode) ==
                ConnectorConfigOptions.ContentRegMode.None.ToString())
            { 
                return new ContentResult
                {
                    ResultType = ContentResultType.Complete
                };
            }

            var result = await PollChanges(connectorConfiguration, channel, cancellationToken);

            return result;

        }
        catch (Exception ex)
        {
            return ContentHelper.FailedResult(string.Empty, ex);
        }
    }

    /// <summary>
    /// Used for all subsequent runs of Content Reg for this channel.
    /// </summary>
    /// <remarks>
    /// The cursor returned from the previous execution of Content Reg is passed into this method.
    /// </remarks>
    public async Task<ContentResult> ContinueAsync(ConnectorConfigModel connectorConfiguration, Channel channel,
        string cursor, IDictionary<string, string> context, CancellationToken cancellationToken)
    {
        try
        {
            if (connectorConfiguration.GetPropertyOrDefault(MetadataNames.ContentRegistrationMode) !=
                ConnectorConfigOptions.ContentRegMode.None.ToString())
            {
                // Outside the Ref Connector, make sure to use the cursor value in this method.
                // (See return value of PollChanges.)
                return await PollChanges(connectorConfiguration, channel, cancellationToken);
            }
            return new ContentResult
            {
                ResultType = ContentResultType.Complete
            };
        }
        catch (Exception ex)
        {
            return ContentHelper.FailedResult(cursor, ex);
        }
    }

    public Task StopAsync(ConnectorConfigModel connectorConfiguration, Channel channel, string cursor,
        CancellationToken cancellationToken)
    {
        // Most connectors won't need to implement this. Refer to method summary.
        return Task.CompletedTask;
    }

    private async Task<ContentResult> PollChanges(ConnectorConfigModel connectorConfiguration, Channel channel,
        CancellationToken cancellationToken)
    {
        channel = await _channelManager.GetFullChannelAsync(connectorConfiguration.Id,
            channel.ExternalId ?? string.Empty, cancellationToken);

        try
        {
            var channelPath = channel.MetaDataItems.First(x => x.Name.Equals(MetadataNames.Path));
            var aggregations = _fileSystemClient.GetAggregations(channelPath.Value);
            
            var sdkAggregations = new List<Aggregation>();
            var sdkRecords = new List<Record>();
            var auditEvents = new List<AuditEvent>();

            foreach (var aggregation in aggregations)
            {
                var (sdkAggregation, _) = SdkConverter.GetSdkAggregation(aggregation, channel);
                var childRecords = _fileSystemClient.GetRecords(aggregation);
                var (sdkChildRecords, recordAuditEvents) = SdkConverter.GetSdkRecords(childRecords, sdkAggregation);
                sdkRecords.AddRange(sdkChildRecords);
                auditEvents.AddRange(recordAuditEvents);
                sdkAggregations.Add(sdkAggregation);
            }

            return new ContentResult
            {
                // The SDK will submit records, aggregations, audit events to Submission services
                // which will then submit them to RecordPoint
                Records = sdkRecords,
                Aggregations = sdkAggregations,
                AuditEvents = auditEvents,

                // If you know there are more objects in the content source remaining to be synced,
                // use ContentResultType.Incomplete instead.
                // The SDK will then schedule this job to run again ASAP.
                ResultType = ContentResultType.Complete

                // If you are using ContentResultType.Incomplete,
                // make sure to set the Cursor here.
                // (Refer Content Sync for an example of using the cursor)
            };
        }
        catch (Exception ex) when (ContentSourceHelper.IsThrottlingException(ex))
        {
            // Every call to the content source should catch throttling exceptions.
            // We want to handle these separately from other types of exceptions,
            // as they mean we need to back off on all operations touching the content source
            // for this tenant. 
            var delay = ContentSourceHelper.GetBackoffTimeSeconds(ex);
            return ContentHelper.ThrottledContentResult(ex, delay);
        }
    }
}