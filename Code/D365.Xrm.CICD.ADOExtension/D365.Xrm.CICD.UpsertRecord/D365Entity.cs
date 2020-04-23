using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.UpsertRecord
{
    class D365Entity
    {
        private CrmServiceClient _crmSvcClient;

        private string _entityName;

        private Guid _entityId;

        private string _userSpecifiedRecordId;

        private string _primaryIdAttributeName;

        private List<D365EntityAttribute> _entityAttributes;

        private RetrieveRecordBy _retrieveBy;

        /// <summary>
        /// Log Message Handler
        /// </summary>
        /// <param name="e">Message Argument</param>
        public delegate void LogMessage(string msg, LogType logType);

        /// <summary>
        /// Log Message Event 
        /// </summary>
        public event LogMessage MessageQueue;

        public D365Entity(
            string entityName, 
            CrmServiceClient crmSvcClient,
            bool createRecord
            )
        {
            this._entityName = entityName;
            this._crmSvcClient = crmSvcClient;
        }

        public void RetrieveRecord(RetrieveRecordBy retrieveBy, string recordGuid, string recordFetchXml, string nameValueJson)
        {
            this._retrieveBy = retrieveBy;

            this.ValidateNameValuePair(nameValueJson);

            this.ValidateAttributeAvailability();

            this.RetrieveEntityInformation(recordGuid, recordFetchXml);
        }

        private void ValidateNameValuePair(string nameValueJson)
        {
            try
            {
                this._entityAttributes = JsonConvert.DeserializeObject<List<D365EntityAttribute>>(nameValueJson);
            }
            catch (Exception)
            {
                throw new Exception("Error occurred while parsing the name value json. Please refer to further documentation at 'https://github.com/vikasrajan82/D365ADOReleaseAutomation/blob/master/README.md'");
            }

            if(this._entityAttributes.Count == 0)
            {
                throw new Exception("There are no attributes to be updated. Please refer to further documentation at 'https://github.com/vikasrajan82/D365ADOReleaseAutomation/blob/master/README.md'");
            }
        }

        private void ValidateAttributeAvailability()
        {
            RetrieveEntityResponse res = this.RetriveEntityMetadata();

            if (res != null)
            {
                this._primaryIdAttributeName = res.EntityMetadata.PrimaryIdAttribute;

                StringBuilder missingAttributes = new StringBuilder();

                foreach (D365EntityAttribute attr in this._entityAttributes)
                {
                    var attributeMetadata = res.EntityMetadata.Attributes.FindAttribute(attr.LogicalName);

                    if (attributeMetadata != null)
                    {
                        attr.MarkAttributeAvailable(
                            attributeMetadata.AttributeType.Value,
                            attributeMetadata.GetType() == typeof(LookupAttributeMetadata) ? ((LookupAttributeMetadata)attributeMetadata).Targets : null);
                    }
                    else
                    {
                        missingAttributes.Append(attr.LogicalName + ",");
                    }
                }

                if (missingAttributes.Length > 0)
                {
                    throw new Exception($"Attribute(s) ({missingAttributes.ToString()}) is missing in the entity {this._entityName}");
                }
                
            }
        }

        private void RetrieveEntityInformation(string recordGuid, string recordFetchXml)
        {
            Entity retrievedRecord = new Entity();

            switch (this._retrieveBy)
            {
                case RetrieveRecordBy.GUID:
                    retrievedRecord = this.RetrieveEntityInformationByGuid(recordGuid);
                    break;
                case RetrieveRecordBy.FetchXML:
                    retrievedRecord = this.RetrieveEntityInformationByFetchXml(recordFetchXml);
                    break;
            }

            if (retrievedRecord != null)
            {
                this.MessageQueue($"Record({retrievedRecord.Id}) matching the specified criteria was found", LogType.Info);

                this._entityId = retrievedRecord.Id;

                foreach (D365EntityAttribute entityAttribute in this._entityAttributes)
                {
                    entityAttribute.SetRetrievedValue(
                        retrievedRecord.Contains(entityAttribute.LogicalName) ? retrievedRecord.Attributes[entityAttribute.LogicalName] : null
                        );
                }
            }
        }

        public Guid UpsertRecord(bool createRecord)
        {
            if (this._entityId != Guid.Empty)
            {
                this.UpdateRecord();
            }
            else
            {
                if(createRecord)
                {
                    this.CreateRecord();
                }
            }

            return this._entityId;
        }

        private void CreateRecord()
        {
            Entity createRecord = new Entity(this._entityName);

            if(!string.IsNullOrEmpty(this._userSpecifiedRecordId) && this._userSpecifiedRecordId.Length > 0)
            {
                createRecord.Id = new Guid(this._userSpecifiedRecordId);
            }

            foreach (D365EntityAttribute entityAttribute in this._entityAttributes)
            {
                createRecord.Attributes.Add(entityAttribute.LogicalName, entityAttribute.NewAttributeValue);
            }

            if (createRecord.Attributes.Count > 0)
            {
                this._entityId = this._crmSvcClient.Create(createRecord);

                this.MessageQueue($"{this._entityName} with guid '{this._entityId}' was successfully created.", LogType.Info);
            }
        }

        private void UpdateRecord()
        {
            Entity updateRecord = new Entity(this._entityName, this._entityId);

            foreach (D365EntityAttribute entityAttribute in this._entityAttributes)
            {
                if (entityAttribute.isAttributeValuesDifferent)
                {
                    updateRecord.Attributes.Add(entityAttribute.LogicalName, entityAttribute.NewAttributeValue);
                }
            }

            if (updateRecord.Attributes.Count > 0)
            {
                this._crmSvcClient.Update(updateRecord);
                this.MessageQueue($"{this._entityName} with guid '{this._entityId}' was successfully updated.", LogType.Info);
            }
            else
            {
                this.MessageQueue($"There is no change to {this._entityName} attribute values...Record was not updated", LogType.Warning);
            }
        }

        private Entity RetrieveEntityInformationByGuid(string recordGuid)
        {
            Guid recordId;

            if (!Guid.TryParse(recordGuid, out recordId))
            {
                this.MessageQueue($"The Guid '{recordGuid}' is not valid. Guid should contain 32 digits with 4 dashes (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)", LogType.TaskError);
                return null;
            }

            this._userSpecifiedRecordId = recordGuid;

            EntityCollection coll = this.ExecuteFetchXmlQuery($"<filter><condition attribute='{this._primaryIdAttributeName}' operator='eq' value='{this._userSpecifiedRecordId}' /></filter>");

            if (coll != null)
            {
                switch (coll.Entities.Count)
                {
                    case 0:
                        this.MessageQueue($"Entity '{this._entityName}' does not contain records with GUID '{this._userSpecifiedRecordId}'", LogType.Warning);
                        break;
                    case 1:
                        return coll.Entities[0];
                    default:
                        if (coll.Entities.Count > 1)
                        {
                            this.MessageQueue($"Entity '{this._entityName}' contains more than 1 records having GUID '{this._userSpecifiedRecordId}'", LogType.Error);
                        }
                        break;
                }
            }

            return null;
        }

        private EntityCollection ExecuteFetchXmlQuery(string fetchFilter)
        {
            StringBuilder queryString = new StringBuilder();
            queryString.Append("<fetch distinct='false' mapping='logical' output-format='xml-platform' version='1.0'>");
            queryString.Append($"<entity name ='{this._entityName}'>");

            foreach (D365EntityAttribute entityAttribute in this._entityAttributes)
            {
                queryString.Append($"<attribute name='{entityAttribute.LogicalName}'/>");
            }

            queryString.Append(fetchFilter);

            queryString.Append("</entity>");
            queryString.Append("</fetch>");

            return this._crmSvcClient.RetrieveMultiple(new FetchExpression(queryString.ToString()));
        }

        private Entity RetrieveEntityInformationByFetchXml(string recordXml)
        {
            EntityCollection coll = this.ExecuteFetchXmlQuery(recordXml);

            if (coll != null)
            {
                switch (coll.Entities.Count)
                {
                    case 0:
                        this.MessageQueue($"There are no records matching the filter criteria ({recordXml}) for the entity '{this._entityName}'", LogType.Warning);
                        break;
                    case 1:
                        return coll.Entities[0];
                    default:
                        if (coll.Entities.Count > 1)
                        {
                            this.MessageQueue($"There are more than 1 records matching the filter criteria ({recordXml}). The update will not proceed further.", LogType.Error);
                        }
                        break;
                }
            }

            return null;
        }

        private RetrieveEntityResponse RetriveEntityMetadata()
        {
            try
            {
                RetrieveEntityRequest req = new RetrieveEntityRequest()
                {
                    EntityFilters = EntityFilters.Attributes,
                    LogicalName = this._entityName,
                    RetrieveAsIfPublished = true
                };

                return (RetrieveEntityResponse)this._crmSvcClient.Execute(req);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("find the entity"))
                {
                    throw new Exception($"The entity {this._entityName} does not exist. Please check again.");
                }
                else
                {
                    throw;
                }
            }


        }
    }
}
