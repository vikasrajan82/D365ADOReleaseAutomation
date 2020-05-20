using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.UpsertRecord
{
    public class D365DocumentTemplates : TaskExtensionBase
    {
        private CrmServiceClient _crmServiceClient;

        private const string DOCUMENT_TEMPLATE_ENTITY_NAME = "documenttemplate";

        public D365DocumentTemplates(string connectionString)
        {
            this._crmServiceClient = new CrmServiceClient(connectionString);
        }

        public void ProcessDocumentTemplates(string configFilePath)
        {
            try
            {
                if (this._crmServiceClient.ConnectedOrgId == null || this._crmServiceClient.ConnectedOrgId == Guid.Empty)
                {
                    throw new Exception("Error occurred while connecting to CDS. Please check the connection string");
                }

                this.LogADOMessage($"Connected to: {this._crmServiceClient.ConnectedOrgFriendlyName}", LogType.Info);

                this.ValidateFilePath(configFilePath);

                string folderPath = Path.GetDirectoryName(configFilePath);

                JArray documentTemplates = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(configFilePath));
                foreach (JToken teamTemplate in documentTemplates)
                {
                    this.UpsertDocumentTemplate(teamTemplate, folderPath);
                }

            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);

                Environment.ExitCode = -1;
            }
        }

        private void ValidateFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception($"Configuration File {filePath} was not found.");
            }

            string fileName = Path.GetFileName(filePath);

            if (!fileName.ToLower().EndsWith(".json"))
            {
                throw new Exception("Output File should have extension of type '.json'");
            }
        }

        private void UpsertDocumentTemplate(JToken documentTemplate, string fileFolderPath)
        {
            try
            {
                Guid docTemplateId;

                if (!Guid.TryParse((string)documentTemplate["documenttemplateid"], out docTemplateId))
                {
                    throw new Exception($"Dcoument Template Id {(string)documentTemplate["documenttemplateid"]} is not a proper GUID.");
                }

                if(documentTemplate["associatedentitytypecode"] == null)
                {
                    throw new Exception($"associatedentitytypecode value does not exist");
                }

                if (documentTemplate["entityname"] == null)
                {
                    throw new Exception($"entityname value does not exist");
                }

                if (documentTemplate["filename"] == null)
                {
                    throw new Exception($"filename value does not exist");
                }

                documentTemplate["content"] = this.ReadDocumentTemplate(documentTemplate["associatedentitytypecode"].ToString(), documentTemplate["entityname"].ToString(), fileFolderPath + "\\" + documentTemplate["filename"].ToString());
                
                D365Entity entity = new D365Entity(DOCUMENT_TEMPLATE_ENTITY_NAME, this._crmServiceClient);

                entity.MessageQueue += LogADOMessage;

                entity.RetrieveRecord(RetrieveRecordBy.GUID, docTemplateId.ToString(), string.Empty, this.GenerateNameValueJson(documentTemplate));

                entity.UpsertRecord(true);
            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);
            }
        }

        private string GenerateNameValueJson(JToken documentTemplate)
        {
            if (documentTemplate == null)
                return string.Empty;

            StringBuilder nameValueJson = new StringBuilder();
            nameValueJson.Append("[");

            if (documentTemplate["name"] != null && !string.IsNullOrEmpty(documentTemplate["name"].ToString()))
            {
                nameValueJson.Append($"{{name:\"name\",value:\"{this.ReplaceSpecialCharacters(documentTemplate["name"].ToString())}\"}},");
            }

            nameValueJson.Append($"{{name:\"documenttype\",value:\"{this.ReplaceSpecialCharacters(documentTemplate["documenttype"].ToString())}\"}},");
            nameValueJson.Append($"{{name:\"associatedentitytypecode\",value:\"{documentTemplate["entityname"].ToString()}\"}},");
            nameValueJson.Append($"{{name:\"content\",value:\"{documentTemplate["content"].ToString()}\"}}");

            nameValueJson.Append("]");

            return nameValueJson.ToString();
        }

        private string ReadDocumentTemplate(string sourceTypeCode, string sourceEntityName, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception($"File '{filePath}' was not found.");
            }

            string targetTypeCode = Convert.ToString(this.GeEntityObjectTypeCode(sourceEntityName, this._crmServiceClient));

            if (sourceTypeCode != targetTypeCode)
            {
                this.ReplaceEntityTypeCodeInDocTemplates(sourceTypeCode, targetTypeCode, filePath);
            }

            return Convert.ToBase64String(File.ReadAllBytes(filePath));
        }

        private void ReplaceEntityTypeCodeInDocTemplates(string sourceTypeCode, string targetTypeCode, string filePath)
        {
            FileStream datazip = new FileStream(filePath, FileMode.Open);
            using (ZipArchive archive = new ZipArchive(datazip, ZipArchiveMode.Update))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName == "word/document.xml" || entry.FullName.Contains("customXml"))
                    {
                        StringBuilder xmlContent;
                        using (TextReader reader = new StreamReader(entry.Open()))
                        {
                            xmlContent = new StringBuilder(reader.ReadToEnd());
                            xmlContent = xmlContent.Replace(sourceTypeCode, targetTypeCode);
                        }

                        using (TextWriter writer = new StreamWriter(entry.Open()))
                        {
                            writer.Write(xmlContent);
                        }
                    }
                }
            }
        }

        private string ReplaceSpecialCharacters(string strValue)
        {
            if (!string.IsNullOrEmpty(strValue))
            {
                return strValue.Replace("\"", "\\\"");
            }

            return strValue;
        }
    }
}
