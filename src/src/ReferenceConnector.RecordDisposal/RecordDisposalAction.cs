using RecordPoint.Connectors.SDK.Caching.Semaphore;
using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.Content;
using RecordPoint.Connectors.SDK.ContentManager;
using RecordPoint.Connectors.SDK.Exceptions;

namespace RecordPoint.Connectors.Reference.RecordDisposal
{
    public class RecordDisposalAction : IRecordDisposalAction
    {
        //This gets triggered when a record linked to our tracked connectors are disposed in the RecordPoint Platform
        public Task<RecordDisposalResult> ExecuteAsync(ConnectorConfigModel connectorConfiguration, Record record,
            CancellationToken cancellationToken)
        {
            //This is where you would implement the logic to delete the record in the content source and upon successfully doing so return a RecordDisposalResult
            try
            {
                var filePath = record.Location;
                if (filePath == null) {
                    return Task.FromResult(new RecordDisposalResult
                    {
                        ResultType = RecordDisposalResultType.Failed,
                        Reason = "Filepath for Record is Null"
                    });
                }
                var file = new FileInfo(filePath);
                if (file.Exists)
                {
                    file.Delete();
                    return Task.FromResult(new RecordDisposalResult
                    {
                        ResultType = RecordDisposalResultType.Complete,
                        Reason = "Record Successfully Deleted"
                    });
                }
                else
                {
                    return Task.FromResult(new RecordDisposalResult
                    {
                        ResultType = RecordDisposalResultType.Deleted,
                        Reason = "Record has already been deleted"
                    });
                }
            }
            //How you would handle a throttling exception
            catch (TooManyRequestsException ex)
            {
                return Task.FromResult(new RecordDisposalResult
                {
                    Exception = ex,
                    ResultType = RecordDisposalResultType.BackOff,
                    SemaphoreLockType = SemaphoreLockType.Scoped,
                    NextDelay = (ex.WaitUntilTime - DateTimeOffset.Now).Seconds
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Task.FromResult(new RecordDisposalResult
                {
                    ResultType = RecordDisposalResultType.Failed,
                    Reason = "Failed to delete record due to exception. Exception message: " + ex.Message
                });
            }
        }
    }
}