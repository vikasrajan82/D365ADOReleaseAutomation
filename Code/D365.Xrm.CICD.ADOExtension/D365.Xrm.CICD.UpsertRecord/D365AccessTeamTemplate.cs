using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.UpsertRecord
{
    public class D365AccessTeamTemplate : TaskExtensionBase
    {
        private CrmServiceClient _crmServiceClient;

        private const string TEAM_TEMPLATE_ENTITY_NAME = "teamtemplate";

        public D365AccessTeamTemplate(
                    string connectionString)
        {
            this._crmServiceClient = new CrmServiceClient(connectionString);
        }

        public void ProcessTeamTemplates(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                this.LogADOMessage($"Configuration File {configFilePath} was not found.", LogType.TaskError);
            }

            try
            {
                JArray teamTemplates = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(configFilePath));
                foreach (JToken teamTemplate in teamTemplates)
                {
                    this.UpsertTeamTemplate(teamTemplate);
                }

            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);

                Environment.ExitCode = -1;
            }
        }

        private void UpsertTeamTemplate(JToken teamTemplate)
        {
            try
            {
                Guid teamTemplateId;

                if (!Guid.TryParse((string)teamTemplate["teamtemplateid"], out teamTemplateId))
                {
                    throw new Exception($"Team Template Id {(string)teamTemplate["teamtemplateid"]} is not a proper GUID.");
                }

                D365Entity entity = new D365Entity(TEAM_TEMPLATE_ENTITY_NAME, this._crmServiceClient);

                entity.MessageQueue += LogADOMessage;

                entity.RetrieveRecord(RetrieveRecordBy.GUID, teamTemplateId.ToString(), string.Empty, this.GenerateNameValueJson(teamTemplate));

                entity.UpsertRecord(true);
            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);
            }
        }

        private string GenerateNameValueJson(JToken teamTemplate)
        {
            if (teamTemplate == null)
                return string.Empty;

            StringBuilder nameValueJson = new StringBuilder();
            nameValueJson.Append("[");

            if (teamTemplate["description"] != null && !string.IsNullOrEmpty(teamTemplate["description"].ToString()))
            {
                nameValueJson.Append($"{{name:\"description\",value:\"{this.ReplaceSpecialCharacters(teamTemplate["description"].ToString())}\"}},"); 
            }

            int objectTypeCode = geEntityObjectTypeCode((string)teamTemplate["entityname"]);

            nameValueJson.Append($"{{name:\"teamtemplatename\",value:\"{this.ReplaceSpecialCharacters(teamTemplate["teamtemplatename"].ToString())}\"}},");
            nameValueJson.Append($"{{name:\"defaultaccessrightsmask\",value:\"{this.ReplaceSpecialCharacters(teamTemplate["defaultaccessrightsmask"].ToString())}\"}},");
            nameValueJson.Append($"{{name:\"objecttypecode\",value:\"{objectTypeCode}\"}}");

            nameValueJson.Append("]");

            return nameValueJson.ToString();
        }

        private string ReplaceSpecialCharacters(string strValue)
        {
            if (!string.IsNullOrEmpty(strValue))
            {
                return strValue.Replace('"', '\"');
            }

            return strValue;
        }

        private int geEntityObjectTypeCode(string entityname)
        {
            MetadataFilterExpression entityFilter = new MetadataFilterExpression();
            entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityname));

            MetadataPropertiesExpression entityProperties = new MetadataPropertiesExpression("ObjectTypeCode")
            {
                AllProperties = false
            };

            EntityQueryExpression entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter,
                Properties = entityProperties
            };

            RetrieveMetadataChangesRequest retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            RetrieveMetadataChangesResponse metadataResponse = (RetrieveMetadataChangesResponse)this._crmServiceClient.Execute(retrieveMetadataChangesRequest);

            if (metadataResponse != null && metadataResponse.EntityMetadata.Count == 1)
            {
                return (int)metadataResponse.EntityMetadata[0].ObjectTypeCode;
            }

            return -1;
        }
    }
}
