using RecordPoint.Connectors.SDK.Abstractions.Content;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.Notifications
{
    /// <summary>
    /// Handles notifications related to Content Registration Requests,
    /// e.g. filtering out excluded Channels from the requests.
    /// </summary>
    /// <remarks>
    /// Included for demo purposes (cannot be triggered for the Reference Connector).
    /// Some connectors may not implement this.
    /// </remarks>
    /// <inheritdoc/>
    public class ContentRegistrationRequestAction : IContentRegistrationRequestAction
    {
        /// <inheritdoc/>
        public Task<List<Channel>> GetChannelsFromRequestAsync(ConnectorConfigModel connectorConfiguration, ContentRegistrationRequest contentRegistrationRequest, CancellationToken cancellationToken)
        {
            // E.g. Use contentRegistrationRequest.Context to determine which channels were requested
            // and then filter out channels that aren't included
            var channels = new List<Channel>();
            return Task.FromResult(channels);
        }
    }
}
