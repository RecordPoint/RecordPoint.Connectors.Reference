using Microsoft.ServiceFabric.Services.Remoting;
using RecordPoint.Connectors.SDK.Client.Models;
using System.Threading.Tasks;

namespace ReferenceConnectorServiceContracts
{
    public interface IReferenceConnectorWorkerService : IService
    {
        /// <summary>
        /// Notifies the Worker service that a ConnectorConfig has been added or was updated.
        /// </summary>
        /// <returns></returns>
        Task UpsertConnectorConfig(ConnectorConfigModel connectorConfig);

        /// <summary>
        /// Notifies the Worker service that a ConnectorConfig was deleted.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeleteConnectorConfig(string id);
    }
}
