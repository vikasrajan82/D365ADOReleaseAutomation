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
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace D365.Xrm.CICD.DataImport
{
    public class D365DataImport : TaskExtensionBase
    {
        private CrmServiceClient _crmServiceClient;

        private string _connectionString;

        private string _configFilePath;

        private List<D365DataZip> _lstDataZip = new List<D365DataZip>();

        private Dictionary<string, string> _guidsEntry;

        private string _guidsJson;

        public D365DataImport(string connectionString, bool displayTraceMessages)
        {
            this._crmServiceClient = new CrmServiceClient(connectionString);
            this._connectionString = connectionString;
            this._showTraceMessages = displayTraceMessages;
        }

        public void ProcessDataImport(string configFilePath, string guidsFilePath)
        {
            try
            {
                if (this._crmServiceClient.ConnectedOrgId == null || this._crmServiceClient.ConnectedOrgId == Guid.Empty)
                {
                    throw new Exception("Error occurred while connecting to CDS. Please check the connection string");
                }

                this.LogADOMessage($"Connected to: {this._crmServiceClient.ConnectedOrgFriendlyName}", LogType.Info);

                this._configFilePath = configFilePath;
                this._guidsJson = guidsFilePath;

                this.IdentifyDataZipToBeImported();

                if (this._lstDataZip.Count > 0)
                {
                    this.RetrieveGuidsToBeReplaced();

                    this.ImportDataZip();
                }
            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);

                this.LogADOMessage(ex.StackTrace, LogType.Debug);

                Environment.ExitCode = -1;
            }
        }

        private void ImportDataZip()
        {
            var initial = InitialSessionState.CreateDefault();
            //initial.ImportPSModule(new[] { "Microsoft.Xrm.Tooling.ConfigurationMigration" });

            using (var runSpace = RunspaceFactory.CreateRunspace(initial))
            {
                runSpace.Open();

                foreach (D365DataZip dataZip in this._lstDataZip)
                {
                    dataZip.ImportMasterData(this._guidsEntry, runSpace, this._connectionString);
                }
            }
        }

        private void RetrieveGuidsToBeReplaced()
        {
            if (!string.IsNullOrEmpty(this._guidsJson))
            {
                if (File.Exists(this._guidsJson))
                {
                    ArrayList entities = retrieveMasterEntitiesGuid();

                    this._guidsEntry = new Dictionary<string, string>();
                    foreach (dynamic entity in entities)
                    {
                        //dictionary to store source guid and target guid for replacement 
                        this._guidsEntry.Add(entity.guid, getEntityGuid(entity.entity, entity.filter));
                    }
                }
            }
        }

        private string getEntityGuid(string entityName, string filterCondition)
        {
            string query = String.Format(@" 
               <fetch>
                <entity name = '{0}'>
                <attribute name = '{0}id'/>
                    <filter>
                        {1}
                    </filter> 
                </entity>
               </fetch>", entityName, filterCondition);

            EntityCollection _coll = this._crmServiceClient.RetrieveMultiple(new FetchExpression(query));

            if (_coll.Entities.Count > 0)
            {
                return _coll.Entities.FirstOrDefault().Id.ToString();
            }

            return null;
        }

        private ArrayList retrieveMasterEntitiesGuid()
        {
            ArrayList lst = new ArrayList();

            JArray entityGuids = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(this._guidsJson));
            foreach (JToken entity in entityGuids)
            {
                dynamic _entityDetails = new System.Dynamic.ExpandoObject();
                _entityDetails.entity = (string)entity["entity"];
                _entityDetails.guid = (string)entity["guid"];
                _entityDetails.filter = (string)entity["filter"];

                lst.Add(_entityDetails);
            }

            return lst;
        }

        private void IdentifyDataZipToBeImported()
        {
            this.LogADOMessage($"Reading the configuration file {Path.GetFileName(this._configFilePath)} to identify the data zip to be installed", LogType.Trace);

            XmlDocument configDoc = new XmlDocument();
            configDoc.Load(this._configFilePath);

            this.LogADOMessage("Successfully read the configuration file", LogType.Trace);

            if (configDoc != null)
            {
                string directoryPath = Path.GetDirectoryName(this._configFilePath);

                XmlNode zipFilesToImport = configDoc.DocumentElement.SelectSingleNode("filestoimport");
                if (zipFilesToImport != null)
                {
                    foreach (XmlNode dataZipFile in zipFilesToImport.SelectNodes("configimportfile"))
                    {
                        var d365DataZip = new D365DataZip(
                            directoryPath + "\\MasterData\\" + dataZipFile.Attributes["filename"].Value,
                            dataZipFile.Attributes["enablebatchmode"] != null ? Convert.ToBoolean(dataZipFile.Attributes["enablebatchmode"].Value) : false,
                            dataZipFile.Attributes["batchsize"] != null ? Convert.ToInt32(dataZipFile.Attributes["batchsize"].Value) : 100,
                            dataZipFile.Attributes["concurrentthread"] != null ? Convert.ToInt32(dataZipFile.Attributes["concurrentthread"].Value) : -1);

                        d365DataZip.MessageQueue += LogADOMessage;

                        this._lstDataZip.Add(d365DataZip);
                    }
                }

                this.LogADOMessage("Data files to be imported: " + this._lstDataZip.Count, LogType.Info);
            }
        }
    }
}
