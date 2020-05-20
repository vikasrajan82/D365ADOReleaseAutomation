using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D365.Xrm.CICD.Base
{
    public class TaskExtensionBase
    {
        protected bool _showTraceMessages;

        private Dictionary<int, string> _objectTypeEntityName;

        protected void SetOutpuParameters(string variableName, string variableValue)
        {
            Console.WriteLine($"##vso[task.setvariable variable={variableName}]{variableValue}");
        }

        private string showInPorgressMessage(string message)
        {
            if (!string.IsNullOrEmpty(message) && message.Contains("===>"))
                return message;

            return $"       ===> {message}";
        }

        protected void LogADOMessage(string message, LogType logType)
        {
            switch (logType)
            {
                case LogType.Warning:
                    Console.WriteLine($"##vso[task.logissue type=warning]{message}");
                    break;
                case LogType.Error:
                    Console.WriteLine($"##[error]{message}");
                    break;
                case LogType.TaskError:
                    Console.WriteLine($"##vso[task.logissue type=error]{message}");
                    Console.WriteLine("##vso[task.complete result=Failed]");
                    break;
                case LogType.Debug:
                    Console.WriteLine($"##[debug]{message.Replace(Environment.NewLine, Environment.NewLine + "##[debug]")}");
                    break;
                case LogType.Info:
                    Console.WriteLine(message);
                    break;
                case LogType.InProgress:
                    Console.WriteLine(this.showInPorgressMessage(message));
                    break;
                case LogType.Trace:
                    if (this._showTraceMessages)
                    {
                        Console.WriteLine(message);
                    }
                    break;

            }
        }

        protected string GetEntityLogicalName(int objectTypeCode, CrmServiceClient crmClient)
        {
            if (this._objectTypeEntityName == null)
            {
                this._objectTypeEntityName = new Dictionary<int, string>();
            }

            if (this._objectTypeEntityName.ContainsKey(objectTypeCode))
            {
                return this._objectTypeEntityName[objectTypeCode];
            }


            RetrieveMetadataChangesResponse retrieveMetadataChangeResponse = this.RetrieveMetadataInformation(RetrieveMetadataBy.ObjectType, objectTypeCode, string.Empty, crmClient);

            if (retrieveMetadataChangeResponse != null && retrieveMetadataChangeResponse.EntityMetadata.Count == 1)
            {
                this._objectTypeEntityName.Add(objectTypeCode, retrieveMetadataChangeResponse.EntityMetadata[0].LogicalName);

                return retrieveMetadataChangeResponse.EntityMetadata[0].LogicalName;
            }

            return null;
        }

        private RetrieveMetadataChangesResponse RetrieveMetadataInformation(RetrieveMetadataBy filter, int objectTypeCode, string entityName, CrmServiceClient crmClient)
        {
            MetadataFilterExpression metadataFilter = new MetadataFilterExpression(LogicalOperator.And);
            switch(filter)
            {
                case RetrieveMetadataBy.ObjectType:
                    metadataFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.Equals, objectTypeCode));
                    break;
                case RetrieveMetadataBy.EntityName:
                    metadataFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityName));
                    break;
            }
            

            MetadataPropertiesExpression metadataProperties = new MetadataPropertiesExpression("ObjectTypeCode")
            {
                AllProperties = false
            };

            EntityQueryExpression metadataQueryExpression = new EntityQueryExpression()
            {
                Criteria = metadataFilter,
                Properties = metadataProperties
            };

            RetrieveMetadataChangesRequest retrieveMetadataChangeRequest = new RetrieveMetadataChangesRequest()
            {
                Query = metadataQueryExpression,
                DeletedMetadataFilters = DeletedMetadataFilters.OptionSet
            };

            return (RetrieveMetadataChangesResponse)crmClient.Execute(retrieveMetadataChangeRequest);
        }

        protected int GeEntityObjectTypeCode(string entityName, CrmServiceClient crmClient)
        {
            if (this._objectTypeEntityName == null)
            {
                this._objectTypeEntityName = new Dictionary<int, string>();
            }

            if (this._objectTypeEntityName.ContainsValue(entityName))
            {
                return this._objectTypeEntityName.FirstOrDefault(x => x.Value == entityName).Key;
            }

            //MetadataFilterExpression entityFilter = new MetadataFilterExpression();
            //entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityname));

            //MetadataPropertiesExpression entityProperties = new MetadataPropertiesExpression("ObjectTypeCode")
            //{
            //    AllProperties = false
            //};

            //EntityQueryExpression entityQueryExpression = new EntityQueryExpression()
            //{
            //    Criteria = entityFilter,
            //    Properties = entityProperties
            //};

            //RetrieveMetadataChangesRequest retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            //{
            //    Query = entityQueryExpression
            //};

            RetrieveMetadataChangesResponse retrieveMetadataChangeResponse = this.RetrieveMetadataInformation(RetrieveMetadataBy.EntityName, -1, entityName, crmClient);

            if (retrieveMetadataChangeResponse != null && retrieveMetadataChangeResponse.EntityMetadata.Count == 1)
            {
                return (int)retrieveMetadataChangeResponse.EntityMetadata[0].ObjectTypeCode;
            }

            return -1;
        }
    }
}
