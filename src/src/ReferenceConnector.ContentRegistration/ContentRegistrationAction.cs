using RecordPoint.Connectors.Reference.Common;
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

    public ContentRegistrationAction(IChannelManager channelManager)
    {
        _channelManager = channelManager;
    }

    public async Task<ContentResult> BeginAsync(ConnectorConfigModel connectorConfiguration, Channel channel,
        IDictionary<string, string> context, CancellationToken cancellationToken)
    {
        try
        {
            if (connectorConfiguration.GetPropertyOrDefault(MetadataNames.ContentRegistrationMode) !=
                ConnectorConfigOptions.ContentRegMode.None.ToString())

            {
                var result = await PollChanges(connectorConfiguration, channel, cancellationToken);

                return result;
            }
            return new ContentResult
            {
                ResultType = ContentResultType.Complete
            };
            
        }
        catch (Exception ex)
        {
            return ContentHelper.FailedResult(string.Empty, ex);
        }
    }

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
            var channelDir = new DirectoryInfo(channelPath.Value);

            var aggregations = channelDir.EnumerateDirectories();
            var sdkAggregations = new List<Aggregation>();
            var sdkRecords = new List<Record>();
            var auditEvents = new List<AuditEvent>();
            foreach (var aggregation in aggregations)
            {
                var (sdkAggregation, _) = SdkConverter.GetSdkAggregation(aggregation, channel);

                var childRecords = aggregation.EnumerateFiles();


                var (sdkChildRecords, recordAuditEvents) = SdkConverter.GetSdkRecords(childRecords, sdkAggregation);

                sdkRecords.AddRange(sdkChildRecords);
                auditEvents.AddRange(recordAuditEvents);
                sdkAggregations.Add(sdkAggregation);
            }

            return new ContentResult
            {
                //The SDK will submit records,aggregations audit events to RecordPoint
                Records = sdkRecords,
                Aggregations = sdkAggregations,

                // If you know there are more objects in the content source remaining to be synced,
                // use ContentResultType.Incomplete instead.
                // The SDK will then schedule this job to run again ASAP.
                ResultType = ContentResultType.Complete,
                // If you are using ContentResultType.Incomplete,
                // make sure to set the Cursor here.
                // (Refer Content Sync for an example of using the cursor)

                AuditEvents = auditEvents
            };
        }
        catch (Exception ex) when (ex is IOException or TimeoutException)
        {
            //These exceptions might result from throttling, this is the assumption which we will make for this connector.
            //The following is an example of how to handle such situations.

            //This represents the time in seconds to wait before retrying the content reg.
            //The SDK will retry the content reg after the specified delay.
            //This is an example default value, for other connectors the exception might even contain the delay
            var delay = 30;
            return ContentHelper.ThrottledContentResult(ex, delay);
        }
    }
}