using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using RecordPoint.Connectors.SDK.Client;
using RecordPoint.Connectors.SDK.Client.Models;
using ReferenceConnectorServiceContracts;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace ReferenceConnectorService.Controllers
{
    [Route("api/[controller]")]
    public class NotificationsController : Controller
    {
        private int GetWorkerPartitionKey(string connectorId)
        {
            // The connector id is a GUID, so all characters will be a hexadecimal digit.
            // Take the first character and convert it to its hexadecimal value. This will yield
            // a number from 0 to 15. 
            return Int32.Parse(connectorId[0].ToString(), NumberStyles.HexNumber);
        }

        /// <summary>
        /// Receives webhook notifications from Records365 vNext.
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task Post([FromBody] ConnectorNotificationModel notification)
        {
            // Handle Connector update events
            if (notification.NotificationType == NotificationType.ConnectorConfigCreated ||
                notification.NotificationType == NotificationType.ConnectorConfigUpdated ||
                notification.NotificationType == NotificationType.ConnectorConfigDeleted)
            {
                // Get a proxy to the partition of the Worker service that will process this connector.
                // This is a mechanism for scaling out the processing load for the connector. In this scheme, connectors
                // are scaled out across the cluster, as the connector ID is used as the partition key. Therefore, all 
                // processing for a single connector will happen on one node. Other partitioning schemes may be used to 
                // scale out in different ways to suit the connector type.
                var worker = ServiceProxy.Create<IReferenceConnectorWorkerService>(new Uri("fabric:/ReferenceConnectorSF/ReferenceConnectorWorkerService"),
                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(GetWorkerPartitionKey(notification.ConnectorId)));

                if (notification.NotificationType == NotificationType.ConnectorConfigCreated ||
                    notification.NotificationType == NotificationType.ConnectorConfigUpdated)
                {
                    await worker.UpsertConnectorConfig(notification.ConnectorConfig);
                }
                else
                {
                    await worker.DeleteConnectorConfig(notification.ConnectorId);
                }
            }
            // Handle ItemDestroyed events
            else if (notification.NotificationType == NotificationType.ItemDestroyed)
            {
                // TODO
            }
                
        }
    }
}
