using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using RecordPoint.Connectors.SDK.Client.Models;
using ReferenceConnectorServiceContracts;
using System.Collections.Concurrent;
using RecordPoint.Connectors.SDK.SubmitPipeline;
using System;
using RecordPoint.Connectors.SDK.Client;

namespace ReferenceConnectorWorkerService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    public class ReferenceConnectorWorkerService : StatefulService, IReferenceConnectorWorkerService
    {
        public ReferenceConnectorWorkerService(StatefulServiceContext context)
            : base(context)
        {
            _connectors = new ConcurrentDictionary<string, ConnectorTask>();
            CreateSubmitPipeline();
        }

        /// <summary>
        /// Create the Submit Pipeline. The submit pipeline can be customized
        /// with custom submit pipeline elements to control the submission
        /// behaviour. The submit pipeline is stateless, so there is only one
        /// instance of each pipeline that is shared across all submissions.
        /// </summary>
        private void CreateSubmitPipeline()
        {
            var apiClientFactory = new ApiClientFactory();

            var httpSubmitItemElement = new HttpSubmitItemPipelineElement(null);
            httpSubmitItemElement.ApiClientFactory = apiClientFactory;
            _itemSubmitPipeline = new FilterPipelineElement(httpSubmitItemElement);

            //ToDO: Need filters for other pipes? Probably.

            var httpSubmitAggregationElement = new HttpSubmitAggregationPipelineElement(null);
            httpSubmitAggregationElement.ApiClientFactory = apiClientFactory;
            _aggregationSubmitPipeline = httpSubmitAggregationElement;

            var httpSubmitBinaryElement = new HttpSubmitBinaryPipelineElement(null);
            httpSubmitBinaryElement.ApiClientFactory = apiClientFactory;
            _binarySubmitPipeline = httpSubmitBinaryElement;

            var httpSubmitAuditEventElement = new HttpSubmitAuditEventPipelineElement(null);
            httpSubmitAuditEventElement.ApiClientFactory = apiClientFactory;
            _auditEventSubmitPipeline = httpSubmitAuditEventElement;
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        /// <summary>
        /// A collection of all connector instances known to the connector.
        /// These would normally be persisted durably, not in memory.
        /// </summary>
        private ConcurrentDictionary<string, ConnectorTask> _connectors;

        /// <summary>
        /// A submit pipeline to submit records to Records365 vNext.
        /// </summary>
        private ISubmission _itemSubmitPipeline;

        /// <summary>
        /// A submit pipeline to submit aggregations to Records365 vNext.
        /// </summary>
        private ISubmission _aggregationSubmitPipeline;

        /// <summary>
        /// A submit pipeline to submit binaries to Records365 vNext.
        /// </summary>
        private ISubmission _binarySubmitPipeline;

        /// <summary>
        /// A submit pipeline to submit audit events to Records365 vNext.
        /// </summary>
        private ISubmission _auditEventSubmitPipeline;

        /// <summary>
        /// A CancellationToken canceled by Service Fabric when it wants to stop the service.
        /// </summary>
        private CancellationToken _serviceFabricCancellationToken;

        public Task UpsertConnectorConfig(ConnectorConfigModel connectorConfig)
        {
            if (_serviceFabricCancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            var connectorTask = _connectors.AddOrUpdate(connectorConfig.Id, 
                new ConnectorTask(connectorConfig, 
                    _itemSubmitPipeline,
                    _aggregationSubmitPipeline,
                    _auditEventSubmitPipeline,
                    _binarySubmitPipeline, 
                    _serviceFabricCancellationToken),
                (x, y) => { y.Update(connectorConfig); return y; } );

            connectorTask.StartIfNecessary();

            return Task.CompletedTask;
        }

        public async Task DeleteConnectorConfig(string id)
        {
            if (_serviceFabricCancellationToken.IsCancellationRequested)
            {
                return;
            }

            _connectors.TryRemove(id, out var removedValue);

            if (removedValue != null)
            {
                await removedValue.Stop().ConfigureAwait(false);
                removedValue.Dispose();
            }
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            _serviceFabricCancellationToken = cancellationToken;

            // TODO: If connector configurations were persisted, we would need to load
            // them here and start their corresponding tasks again. 

            while (!_serviceFabricCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            // The service fabric cancellation token has been canceled, meaning SF wants to stop 
            // this replica. Wait for any running connector tasks to stop.
            foreach( var connectorTask in _connectors)
            {
                await connectorTask.Value.Stop().ConfigureAwait(false);
            }
        }
    }
}
