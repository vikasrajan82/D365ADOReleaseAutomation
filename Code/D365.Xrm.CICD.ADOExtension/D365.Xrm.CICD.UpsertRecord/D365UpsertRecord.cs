using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.UpsertRecord
{
    public class D365UpsertRecord : TaskExtensionBase
    {
        private CrmServiceClient _crmServiceClient;

        private string _entityName;

        private string _nameValueJson;

        private bool _createRecord;

        public D365UpsertRecord(
            string connectionString, 
            string entityName,
            string nameValueJson,
            bool createRecord)
        {
            this._crmServiceClient = new CrmServiceClient(connectionString);
            this._entityName = entityName;
            this._nameValueJson = nameValueJson;
            this._createRecord = createRecord;
        }

        private void ProcessUpsertOperation(
            RetrieveRecordBy filterType,
            string recordId,
            string recordFetchXml)
        {
            try
            {
                D365Entity entity = new D365Entity(this._entityName, this._crmServiceClient);

                entity.MessageQueue += LogADOMessage;

                entity.RetrieveRecord(filterType, recordId, recordFetchXml, this._nameValueJson);

                Guid entityId = entity.UpsertRecord(this._createRecord);

                if (entityId != Guid.Empty)
                {
                    this.LogADOMessage("Updating the output variables", LogType.Trace);

                    this.SetOutpuParameters("UPSERT_RECORDID", entityId.ToString());
                }
            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);

                Environment.ExitCode = -1;
            }
        }

        public void ProcessRecordByGuid(string recordId)
        {
            this.ProcessUpsertOperation(RetrieveRecordBy.GUID, recordId, string.Empty);
        }

        public void ProcessRecordByFetchXml(string recordFetchXml)
        {
            this.ProcessUpsertOperation(RetrieveRecordBy.FetchXML, string.Empty, recordFetchXml);
        }
    }
}
