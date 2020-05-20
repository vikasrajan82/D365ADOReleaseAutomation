using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.RetrieveRecord
{
    public class D365RetrieveDocumentTemplates : TaskExtensionBase
    {
        private CrmServiceClient _crmServiceClient;

        private string _destinationPath;

        private const string DOCUMENT_TEMPLATE_ENTITY_NAME = "documenttemplate";

        private ArrayList _lstDocumentTemplates;

        public D365RetrieveDocumentTemplates(string connectionString, string destinationFolder)
        {
            this._crmServiceClient = new CrmServiceClient(connectionString);
            this._destinationPath = destinationFolder;
            this._lstDocumentTemplates = new ArrayList();
        }

        public void GenerateAllTemplates()
        {
            this.GenerateTemplates(string.Empty);
        }

        public void GenerateTemplates(string documentNames)
        {
            try
            {
                this.ValidateFileLocation(this._destinationPath);

                JArray inputDocumentNames = this.ParseDocumentNames(documentNames);

                EntityCollection retrievedTemplates = this.RetrieveTemplateEntity(inputDocumentNames);

                if (retrievedTemplates == null || retrievedTemplates.Entities == null || retrievedTemplates.Entities.Count == 0)
                {
                    this.LogADOMessage("There are no document templates available to be processed.", LogType.Warning);

                    return;
                }

                if (inputDocumentNames == null)
                {
                    this.ExportDocumentTemplates(retrievedTemplates);
                }
                else
                {
                    foreach (JToken inputDocument in inputDocumentNames)
                    {
                        string docName = inputDocument.Value<string>();
                        Entity documentTemplate = retrievedTemplates.Entities.Where(x => x.Attributes["name"].ToString() == docName).FirstOrDefault<Entity>();

                        if (documentTemplate == null)
                        {
                            this.LogADOMessage($"There is no document template with the name '{docName}'", LogType.Warning);
                        }
                        else
                        {
                            this.ExportTemplate(documentTemplate);
                        }
                    }
                }

                File.WriteAllText(this._destinationPath + "\\DocumentTemplates.json", JsonConvert.SerializeObject(this._lstDocumentTemplates));
            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);

                Environment.ExitCode = -1;
            }
        } 


        private void ValidateFileLocation(string filePath)
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
        }

        private void ExportDocumentTemplates(EntityCollection documentTemplatesCollection)
        {
            foreach (Entity docTemplate in documentTemplatesCollection.Entities)
            {
                this.ExportTemplate(docTemplate);
            }
        }

        private void ExportTemplate(Entity docTemplate)
        {
            try
            {
                D365DocumentTemplate _docTemplate = new D365DocumentTemplate(docTemplate);

                _docTemplate.MessageQueue += this.LogADOMessage;

                this._lstDocumentTemplates.Add(
                    _docTemplate.ProcessDocument(
                        this._destinationPath,
                        this.GeEntityObjectTypeCode(_docTemplate.DocumentEntityName, this._crmServiceClient))
                    );

                this.LogADOMessage($"Document '{_docTemplate.Name}' was successfully exported", LogType.Info);
            }
            catch (Exception ex)
            {
                this.LogADOMessage($"Error occurred while processing document template ({docTemplate.Id.ToString()}): {ex.Message}", LogType.Error);
            }
        }

        private JArray ParseDocumentNames(string documentNames)
        {
            JArray templateNames = null;
            try
            {
                if (!string.IsNullOrEmpty(documentNames.Trim()))
                {
                    templateNames = (JArray)JsonConvert.DeserializeObject(documentNames);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while reading the document names; {ex.Message}");
            }

            return templateNames;
        }

        private EntityCollection RetrieveTemplateEntity(JArray templateNames)
        {
            QueryExpression qeTemplateNames = new QueryExpression();
            qeTemplateNames.EntityName = DOCUMENT_TEMPLATE_ENTITY_NAME;
            qeTemplateNames.ColumnSet = new ColumnSet(new string[] { "associatedentitytypecode", "languagecode", "documenttemplateid", "documenttype", "name", "content", "status", "createdby" });

            if (templateNames != null)
            {
                qeTemplateNames.Criteria.FilterOperator = LogicalOperator.Or;
                foreach (JToken templateName in templateNames)
                {
                    qeTemplateNames.Criteria.AddCondition("name", ConditionOperator.Equal, templateName.Value<string>());
                }
            }

            return this._crmServiceClient.RetrieveMultiple(qeTemplateNames);
        }
    }
}
