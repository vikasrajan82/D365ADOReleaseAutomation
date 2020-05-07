using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.RetrieveRecord
{
    public class D365RetrieveAccessTeams : TaskExtensionBase
    {
        private CrmServiceClient _crmServiceClient;

        private const string TEAM_TEMPLATE_ENTITY_NAME = "teamtemplate";

        private Dictionary<int, string> _objectTypeEntityName;

        private List<D365AccessTeamTemplate> _lstAccessTeams;

        public D365RetrieveAccessTeams(string connectionString)
        {
            this._crmServiceClient = new CrmServiceClient(connectionString);
        }

        public void GenerateAccessTeamTemplatesJson(string destinationFilePath)
        {
            try
            {
                destinationFilePath = this.ValidateFileLocation(destinationFilePath);

                EntityCollection accessTeamTemplates = this.RetrieveAccessTeamTemplates();

                if (accessTeamTemplates == null || accessTeamTemplates.Entities == null || accessTeamTemplates.Entities.Count == 0)
                {
                    this.LogADOMessage("No record exists for Access Team templates", LogType.Warning);

                    return;
                }

                this.LogADOMessage($"Retrieved {accessTeamTemplates.Entities.Count} Access Team Templates", LogType.Info);

                this._objectTypeEntityName = new Dictionary<int, string>();
                this._lstAccessTeams = new List<D365AccessTeamTemplate>();

                foreach (Entity accessTeamTemplate in accessTeamTemplates.Entities)
                {
                    if (accessTeamTemplate.Attributes["teamtemplatename"] != null)
                    {
                        D365AccessTeamTemplate accessTemplateRecord = new D365AccessTeamTemplate()
                        {
                            Id = accessTeamTemplate.Id,
                            Name = accessTeamTemplate.Attributes["teamtemplatename"].ToString(),
                            Description = accessTeamTemplate.Contains("description") && accessTeamTemplate.Attributes["description"] != null ? accessTeamTemplate.Attributes["description"].ToString() : null,
                            DefaultAccessRightsMask = accessTeamTemplate.Contains("defaultaccessrightsmask") && accessTeamTemplate.Attributes["defaultaccessrightsmask"] != null ? accessTeamTemplate.Attributes["defaultaccessrightsmask"].ToString() : null,
                            EntityName = accessTeamTemplate.Contains("objecttypecode") ? this.GetEntityLogicalName(accessTeamTemplate.GetAttributeValue<int>("objecttypecode")) : null
                        };

                        this._lstAccessTeams.Add(accessTemplateRecord);
                    }
                    else
                    {
                        this.LogADOMessage($"Access Team Template with id '{accessTeamTemplate.Id}' does not have a name associated with it. This team template has been skipped.", LogType.Warning);
                    }
                }

                File.WriteAllText(destinationFilePath, JsonConvert.SerializeObject(this._lstAccessTeams));

                this.LogADOMessage($"Access Team Template file ({destinationFilePath}) was created", LogType.Info);
            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);

                Environment.ExitCode = -1;
            }
        }

        private string ValidateFileLocation(string filePath)
        {
            string parentFolder = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                if (!fileName.ToLower().EndsWith(".json"))
                {
                    throw new Exception("Output File should have extension of type '.json'");
                }
            }
            else
            {
                fileName = "TeamTemplates.json";
            }

            if(!Directory.Exists(parentFolder))
            {
                Directory.CreateDirectory(parentFolder);
            }

            return $"{parentFolder}\\{fileName}";
        }

        private string GetEntityLogicalName(int objectTypeCode)
        {
            if (this._objectTypeEntityName == null)
                return null;

            if (this._objectTypeEntityName.ContainsKey(objectTypeCode))
            {
                return this._objectTypeEntityName[objectTypeCode];
            }

            
            RetrieveMetadataChangesResponse retrieveMetadataChangeResponse = this.RetrieveMetadataInformation(objectTypeCode);

            if (retrieveMetadataChangeResponse != null && retrieveMetadataChangeResponse.EntityMetadata.Count == 1)
            {
                this._objectTypeEntityName.Add(objectTypeCode, retrieveMetadataChangeResponse.EntityMetadata[0].LogicalName);

                return retrieveMetadataChangeResponse.EntityMetadata[0].LogicalName;
            }

            return null;
        }

        private RetrieveMetadataChangesResponse RetrieveMetadataInformation(int objectTypeCode)
        {
            MetadataFilterExpression objectTypeMetadataFilter = new MetadataFilterExpression(LogicalOperator.And);
            objectTypeMetadataFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.Equals, objectTypeCode));

            MetadataPropertiesExpression metadataProperties = new MetadataPropertiesExpression("ObjectTypeCode")
            {
                AllProperties = false
            };

            EntityQueryExpression metadataQueryExpression = new EntityQueryExpression()
            {
                Criteria = objectTypeMetadataFilter,
                Properties = metadataProperties
            };

            RetrieveMetadataChangesRequest retrieveMetadataChangeRequest = new RetrieveMetadataChangesRequest()
            {
                Query = metadataQueryExpression,
                DeletedMetadataFilters = DeletedMetadataFilters.OptionSet
            };

            return (RetrieveMetadataChangesResponse)this._crmServiceClient.Execute(retrieveMetadataChangeRequest);
        }

        private EntityCollection RetrieveAccessTeamTemplates()
        {
            QueryExpression accessTeamTemplateQuery = new QueryExpression();
            accessTeamTemplateQuery.EntityName = TEAM_TEMPLATE_ENTITY_NAME;
            accessTeamTemplateQuery.ColumnSet = new ColumnSet(new string[] { "teamtemplateid", "teamtemplatename", "description", "defaultaccessrightsmask", "objecttypecode" });

            return this._crmServiceClient.RetrieveMultiple(accessTeamTemplateQuery);
        }

    }
}
