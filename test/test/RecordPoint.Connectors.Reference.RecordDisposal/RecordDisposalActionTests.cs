using RecordPoint.Connectors.SDK.Client.Models;
using RecordPoint.Connectors.SDK.ContentManager;
using Record = RecordPoint.Connectors.SDK.Content.Record;

namespace RecordPoint.Connectors.Reference.RecordDisposal
{
    //This an example test and the testing suite is a stub to show the conventions we follow, in practice we would have a more comprehensive test suite.
    //Some connectors have integration tests too.
    public class RecordDisposalActionTests
    {
        
        [Fact(DisplayName = "RecordDisposalAction.Execute method returns RecordDisposalResult of type Failed when location is null")]
        public void RecordDisposalExecuteFailsWhenRecordLocationIsNull()
        {
            //Arrange
            var connectorConfigModel = new ConnectorConfigModel();
            var recordDisposalAction = new RecordDisposalAction();
            var record = new Record()
            {
                Location = null
            };
            var result = recordDisposalAction.ExecuteAsync(connectorConfigModel, record, CancellationToken.None);
            Assert.Equal(RecordDisposalResultType.Failed, result.Result.ResultType);
        }
    }
}