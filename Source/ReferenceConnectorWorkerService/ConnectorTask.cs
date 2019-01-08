using RecordPoint.Connectors.SDK;
using RecordPoint.Connectors.SDK.Client;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.SubmitPipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReferenceConnectorWorkerService
{
    /// <summary>
    /// Represents a long running task that runs for one instance of the Connector.
    /// </summary>
    public class ConnectorTask : IDisposable
    {
        private ConnectorConfigModel _connectorConfigModel;
        private ISubmission _itemSubmitPipeline;
        private ISubmission _aggregationSubmitPipeline;
        private ISubmission _auditEventSubmitPipeline;
        private ISubmission _binarySubmitPipeline;
        private Task _task;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _linkedTokenSource;
        private CancellationToken _outerCancellationToken;
        private object _syncRoot;

        public ConnectorTask(ConnectorConfigModel connectorConfigModel,
            ISubmission itemSubmitPipeline,
            ISubmission aggregationSubmitPipeline,
            ISubmission auditEventSubmitPipeline,
            ISubmission binarySubmitPipeline,
            CancellationToken cancellationToken)
        {
            _connectorConfigModel = connectorConfigModel;
            _itemSubmitPipeline = itemSubmitPipeline;
            _aggregationSubmitPipeline = aggregationSubmitPipeline;
            _auditEventSubmitPipeline = auditEventSubmitPipeline;
            _binarySubmitPipeline = binarySubmitPipeline;
            _outerCancellationToken = cancellationToken;
            _syncRoot = new object();
        }

        public void StartIfNecessary()
        {
            if (_task == null)
            {
                lock(_syncRoot)
                {
                    if (_task == null)
                    {
                        _cancellationTokenSource = new CancellationTokenSource();

                        _linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, _outerCancellationToken);

                        _task = Run(_linkedTokenSource.Token);
                    }
                }   
            }
        }

        public async Task Stop()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                await _task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Update(ConnectorConfigModel model)
        {
            _connectorConfigModel = model;
        }

        private async Task Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Only run for connectors that are enabled
                if (_connectorConfigModel.Status == ConnectorConfigStatus.Enabled)
                {
                    await SubmitItem(cancellationToken).ConfigureAwait(false);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SubmitItem(CancellationToken cancellationToken)
        {
            var submitContext = new SubmitContext
            {
                TenantId = _connectorConfigModel.TenantIdAsGuid,
                ConnectorConfigId = _connectorConfigModel.IdAsGuid,
                ApiClientFactorySettings = ConnectorApiAuthHelper.GetApiClientFactorySettings(),
                AuthenticationHelperSettings = ConnectorApiAuthHelper.GetAuthenticationHelperSettings(_connectorConfigModel.TenantDomainName),
                CoreMetaData = new List<SubmissionMetaDataModel>(),
                SourceMetaData = new List<SubmissionMetaDataModel>(),
                Filters = _connectorConfigModel.Filters,
                CancellationToken = cancellationToken
            };

            // Set the "ExternalId" of the item.
            // ExternalId is an ID that uniquely identifies this item in the content source. 
            // Multiple submissions of items with the same ExternalId will be treated as different
            // versions of the same item. 
            // Submissions of items with different ExternalId values will be treated as different items.
            // In this sample, we generate a new ExternalId for every submission, meaning every submission
            // is a new item.
            var externalId = Guid.NewGuid().ToString();
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.ExternalId, value: externalId));

            // Set the "ParentExternalId" of the item.
            // For items this field identifies the parent item. 
            var parentExternalId = Guid.NewGuid().ToString();
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.ParentExternalId, value: parentExternalId));

            // Set the "SourceLastModifiedDate" of the item.
            // Records365 vNext uses this field to determine ordering of versions of an item. Note that items may be submitted 
            // out of order.
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.SourceLastModifiedDate, value: DateTime.UtcNow.ToString("O")));

            // Set some mandatory core fields that are required by all items.
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.Title, value: $"Fake Record from {_connectorConfigModel.Id}"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.Author, value: "Record ReferenceConnectorSF"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.SourceCreatedDate, value: DateTime.UtcNow.ToString("O")));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.SourceCreatedBy, value: "ReferenceConnectorSF"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.SourceLastModifiedBy, value: "ReferenceConnectorSF"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.Location, value: "Fake record"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.MediaType, value: "Electronic"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.ContentVersion, value: "1.0"));

            // Set some source metadata fields. 
            // Source metadata fields are intended for any metadata related to the item available from the content source that is 
            // not directly captured by the core metadata. Records365 vNext makes no assumption about the presence or nature of any
            // source metadata. Any source field submitted to Records365 vNext is visible in the UI and is searchable by users. 
            submitContext.SourceMetaData.Add(new SubmissionMetaDataModel("Checkin Comments", type: nameof(String), value: "Some checkin comments"));
            submitContext.SourceMetaData.Add(new SubmissionMetaDataModel("Last Sync Date", type: nameof(DateTime), value: DateTime.UtcNow.ToString("O")));

            // Submit the item!
            try
            {
                await _itemSubmitPipeline.Submit(submitContext).ConfigureAwait(false);

                HandleSubmitPipelineResult(submitContext);
            }
            catch (Exception)
            {
                // Something went wrong trying to submit the item. 
                // Dead-letter the item to a durable data store where it can be retried later. (e.g., a message broker).
            }

            //If filtered out or otherwise skipped, we do not wish to trigger the following consequential submissions.
            //Note that this may be done as part of HandleSubmitPipelineResult for neater code.
            if (submitContext.SubmitResult.SubmitStatus == SubmitResult.Status.OK)
            {
                // After submitting, check to see if Records365 vNext has a record of the parent item that was referenced
                // in the submission above. If it doesn't, we need to submit it.
                if (submitContext.AggregationFoundDuringItemSubmission.HasValue &&
                !submitContext.AggregationFoundDuringItemSubmission.Value)
                {
                    await SubmitAggregation(parentExternalId, cancellationToken).ConfigureAwait(false);
                }

                // Submit an audit event for the item.
                await SubmitAuditEvent(externalId, cancellationToken).ConfigureAwait(false);

                // Submit the binary for the item.
                await SubmitBinary(externalId, cancellationToken).ConfigureAwait(false);
            }
            
        }

        private async Task SubmitAggregation(string externalId, CancellationToken cancellationToken)
        {
            var submitContext = new SubmitContext
            {
                TenantId = _connectorConfigModel.TenantIdAsGuid,
                ConnectorConfigId = _connectorConfigModel.IdAsGuid,
                ApiClientFactorySettings = ConnectorApiAuthHelper.GetApiClientFactorySettings(),
                AuthenticationHelperSettings = ConnectorApiAuthHelper.GetAuthenticationHelperSettings(_connectorConfigModel.TenantDomainName),
                CoreMetaData = new List<SubmissionMetaDataModel>(),
                SourceMetaData = new List<SubmissionMetaDataModel>(),
                Filters = _connectorConfigModel.Filters,
                CancellationToken = cancellationToken
            };
            
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.ExternalId, value: externalId.ToString()));

            // Aggregations need to have the special "ItemTypeId" field set to "1" to identify them as being an aggregation. 
            // Submissions that omit this field are identified as items.
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.ItemTypeId, value: "1"));

            // Note we do not set the ParentExternalId of the aggregation - in most cases aggregations don't have a parent.

            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.SourceLastModifiedDate, value: DateTime.UtcNow.ToString("O")));

            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.Title, value: $"Fake Record Folder from {_connectorConfigModel.Id}"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.Author, value: "Record ReferenceConnectorSF"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.SourceCreatedDate, value: DateTime.UtcNow.ToString("O")));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.SourceCreatedBy, value: "ReferenceConnectorSF"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.SourceLastModifiedBy, value: "ReferenceConnectorSF"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.Location, value: "Fake Record Folder"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.MediaType, value: "Electronic"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.ContentVersion, value: "1.0"));

            submitContext.SourceMetaData.Add(new SubmissionMetaDataModel("Checkin Comments", type: nameof(String), value: "Some checkin comments"));
            
            // Submit the aggregation!
            try
            {
                await _aggregationSubmitPipeline.Submit(submitContext).ConfigureAwait(false);

                HandleSubmitPipelineResult(submitContext);
            }
            catch (Exception)
            {
                // Something went wrong trying to submit the item. 
                // Dead-letter the item to a durable data store where it can be retried later. (e.g., a message broker).
            }
        }
        
        private async Task SubmitAuditEvent(string itemExternalId, CancellationToken cancellationToken)
        {
            var submitContext = new SubmitContext
            {
                TenantId = _connectorConfigModel.TenantIdAsGuid,
                ConnectorConfigId = _connectorConfigModel.IdAsGuid,
                ApiClientFactorySettings = ConnectorApiAuthHelper.GetApiClientFactorySettings(),
                AuthenticationHelperSettings = ConnectorApiAuthHelper.GetAuthenticationHelperSettings(_connectorConfigModel.TenantDomainName),
                CoreMetaData = new List<SubmissionMetaDataModel>(),
                SourceMetaData = new List<SubmissionMetaDataModel>(),
                Filters = _connectorConfigModel.Filters,
                CancellationToken = cancellationToken
            };

            // Associate the audit event with the item with the item external id.
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.AuditEvent.ExternalId, value: itemExternalId));

            // Set the EventExternalId - this is an ID that uniquely identifies the audit event in the content source.
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.AuditEvent.EventExternalId, value: Guid.NewGuid().ToString()));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.AuditEvent.EventType, value: "Reference Audit Event"));

            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.AuditEvent.UserName, value: "Record ReferenceConnectorSF"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.AuditEvent.UserId, value: "Record ReferenceConnectorSF"));
            submitContext.CoreMetaData.Add(new SubmissionMetaDataModel(Fields.AuditEvent.Created, value: DateTime.UtcNow.ToString("O")));
            
            // Set source metadata on the audit event
            submitContext.SourceMetaData.Add(new SubmissionMetaDataModel("Checkin Comments", type: nameof(String), value: "Some checkin comments"));

            // Submit the audit event!
            try
            {
                await _auditEventSubmitPipeline.Submit(submitContext).ConfigureAwait(false);

                HandleSubmitPipelineResult(submitContext);
            }
            catch (Exception)
            {
                // Something went wrong trying to submit the item. 
                // Dead-letter the item to a durable data store where it can be retried later. (e.g., a message broker).
            }
        }

        private async Task SubmitBinary(string itemExternalId, CancellationToken cancellationToken)
        {
            BinarySubmitContext binarySubmitContext = null;

            // Submit the binary!
            // Note the retry loop here - the binary submission endpoint may reject the submission
            // of a binary if the item it refers to has not been processed by the platform yet. 
            int tryCount = 0;
            do
            {
                tryCount++;

                if (binarySubmitContext == null)
                {
                    binarySubmitContext = new BinarySubmitContext
                    {
                        TenantId = _connectorConfigModel.TenantIdAsGuid,
                        ConnectorConfigId = _connectorConfigModel.IdAsGuid,
                        ApiClientFactorySettings = ConnectorApiAuthHelper.GetApiClientFactorySettings(),
                        AuthenticationHelperSettings = ConnectorApiAuthHelper.GetAuthenticationHelperSettings(_connectorConfigModel.TenantDomainName),
                        CoreMetaData = new List<SubmissionMetaDataModel>(),
                        SourceMetaData = new List<SubmissionMetaDataModel>(),
                        Filters = _connectorConfigModel.Filters,
                        CancellationToken = cancellationToken
                    };

                    // Associate the binary with the item with the ItemExternalId field.
                    binarySubmitContext.ItemExternalId = itemExternalId;

                    // Set the "ExternalId" of the binary. The ExternalId uniquely identifies the binary in the content
                    // source. Note that this is not the same thing as the ExternalId of the item. An item may have 
                    // multiple binaries associated with it - if multiple binaries are submitted, end users can download them
                    // as a zipped archive.
                    binarySubmitContext.ExternalId = Guid.NewGuid().ToString();

                    // Set the "FileName" of the binary. This field is optional. When it is provided, end users will 
                    // get this filename by default when they download the binary from Records365 vNext.
                    binarySubmitContext.FileName = "file.txt";
                }

                var fileContent = "This is a binary file!";
                var fileBytes = Encoding.Default.GetBytes(fileContent);
                using (var stream = new MemoryStream(fileBytes))
                {

                    // Set the Stream of the binary. This stream represents the binary content.
                    // Note that the MemoryStream.Position will be moved to the end on each API call
                    // So if the submission needs to be retried, a new stream should be assigned here
                    binarySubmitContext.Stream = stream;

                    try
                    {
                        await _binarySubmitPipeline.Submit(binarySubmitContext).ConfigureAwait(false);
                        HandleSubmitPipelineResult(binarySubmitContext);
                    }
                    catch (Exception)
                    {
                        // Something went wrong trying to submit the item. 
                        // Dead-letter the item to a durable data store where it can be retried later. (e.g., a message broker).
                    }

                    // If the submit was deferred by the platform, wait a few seconds and try again.
                    if (binarySubmitContext.SubmitResult.SubmitStatus == SubmitResult.Status.Deferred)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    }
                }
            }
            while (binarySubmitContext.SubmitResult.SubmitStatus == SubmitResult.Status.Deferred && tryCount < 30);
        }

        private void HandleSubmitPipelineResult(SubmitContext submitContext)
        {
            // Check the result of the submission.
            if (submitContext.SubmitResult.SubmitStatus == SubmitResult.Status.OK)
            {
                // The item was submitted successfully! Records365 guarantees that the item is now stored durably and
                // the connector has no need to store or queue this item any more.
            }
            else if (submitContext.SubmitResult.SubmitStatus == SubmitResult.Status.Skipped)
            {
                // The item was skipped by the submit pipeline - it may have been filtered out. The connector has no need to store
                // or queue this item any more.
            }
            else if (submitContext.SubmitResult.SubmitStatus == SubmitResult.Status.Deferred)
            {
                // The submit pipeline attempted to submit the item and the pipeline returned a result indicating that the item
                // should be tried again later.
            }
            else if (submitContext.SubmitResult.SubmitStatus == SubmitResult.Status.ConnectorDisabled)
            {
                // The submit pipeline attempted to submit the item and the Records365 Connector API returned a response
                // that indicated that the connector was disabled in Records365. All processing for this connector instance
                // should stop if this result is received.
                // Note that a user may re-enable the connector in Records365, so any book-keeping related to the connector
                // instance should be kept. Items should be kept in a durable store with some capability to retry them.
            }
            else if (submitContext.SubmitResult.SubmitStatus == SubmitResult.Status.ConnectorNotFound)
            {
                // The submit pipeline attempted to submit the item and the Records365 Connector API returned a response
                // that indicated that the connector doesn't exist in Records365. All processing for this connector instance
                // should stop if this result is received. Any state related to the connector, including items, can be discarded.
            }
        }

        public void Dispose()
        {
            _linkedTokenSource?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
