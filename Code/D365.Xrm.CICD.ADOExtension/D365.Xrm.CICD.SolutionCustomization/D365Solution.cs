using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace D365.Xrm.CICD.SolutionCustomization
{
    public class D365Solution
    {
        private const string SOLUTION_XML_FILENAME = "solution.xml";

        private string _filePath;

        private string _fileName;

        private string _uniqueName;

        private string _versionNumber;

        private bool _isManaged;

        private bool _isUpgradePending;

        private bool _overwriteUnmanagedCustomization;

        private bool _activatePlugins;

        private bool _replaceGuids;

        private bool _importAsHoldingSolutions;

        private CrmServiceClient _crmSvcClient;

        private Guid _asyncOperationId;

        private Guid _importJobId;

        /// <summary>
        /// Log Message Handler
        /// </summary>
        /// <param name="e">Message Argument</param>
        public delegate void LogMessage(string msg, LogType logType);

        /// <summary>
        /// Log Message Event 
        /// </summary>
        public event LogMessage MessageQueue;

        private bool IsSolutionInstalled()
        {
            QueryExpression qe = new QueryExpression();
            qe.EntityName = "solution";
            qe.ColumnSet = new ColumnSet("solutionid", "uniquename");

            qe.Criteria = new FilterExpression();
            qe.Criteria.AddCondition("uniquename", ConditionOperator.BeginsWith, this._uniqueName);
            qe.Criteria.AddCondition("version", ConditionOperator.Equal, this._versionNumber);

            EntityCollection col = this._crmSvcClient.RetrieveMultiple(qe);

            if (col != null && col.Entities != null && col.Entities.Count > 0)
            {
                if (col.Entities[0].Attributes["uniquename"] != null && col.Entities[0].Attributes["uniquename"].ToString().Contains("_Upgrade"))
                {
                    this._isUpgradePending = true;
                }

                return true;
            }

            return false;
        }

        public void ValidateSolutionFile()
        {
            if (!File.Exists(this._filePath))
            {
                throw new Exception($"File '{this._filePath}' does not exist. Please verify the file location.");
            }

            this.MessageQueue($"File Location '{this._filePath}' is valid", LogType.Trace);

            this._fileName = Path.GetFileName(this._filePath);
        }

        public void ImportSolution(CrmServiceClient svcClient)
        {
            this._crmSvcClient = svcClient;

            this.GetSolutionPackageDetails();

            if (!string.IsNullOrEmpty(this._uniqueName))
            {
                if (this.IsSolutionInstalled())
                {
                    this.MessageQueue($"Skipping Solution Install for {this._fileName}", LogType.Warning);

                    if(!this._importAsHoldingSolutions)
                    {
                        this._isUpgradePending = false;
                    }
                }
                else
                {
                    if (!this.CheckSolutionInstallationInProgress())
                    {
                        this.MessageQueue($"Installing Solution {this._fileName}", LogType.Info);

                        this._asyncOperationId = this._crmSvcClient.ImportSolutionToCrmAsync(
                            this._filePath, 
                            out this._importJobId, 
                            this._activatePlugins, 
                            this._overwriteUnmanagedCustomization, 
                            false, 
                            this._importAsHoldingSolutions);
                    }

                    this.DisplaySolutionInstallProgress(this._asyncOperationId, this._importJobId, this._fileName, this._uniqueName);

                    if(this._importAsHoldingSolutions)
                    {
                        this._isUpgradePending = true;
                    }
                }

                if (this._isUpgradePending)
                {
                    this.UpgradeSolution();
                }
            }
            else
            {
                throw new Exception($"Solution '{this._fileName}' cannot be read. Please verify if the file is a valid D365 solution file");
            }
        }

        private void UpgradeSolution()
        {
            Guid importId = this._crmSvcClient.DeleteAndPromoteSolutionAsync(this._uniqueName);

            bool importCompleted = false;

            this.MessageQueue($"Upgrading the Solution: {this._fileName}", LogType.Info);

            while (!importCompleted)
            {
                Entity ent = this.GetAsyncOperationId(importId);

                if (ent != null
                    && ent.Contains("completedon")
                    && ent.Attributes["completedon"] != null)
                {
                    importCompleted = true;

                    if (ent.Contains("statuscode") && ent.Attributes["statuscode"] != null && ent.Attributes["statuscode"].ToString() == "31")
                    {
                        throw new Exception($"Upgrade of Solution: {this._fileName} has failed. Please check the 'Solution History' for more details");
                    }

                    this.MessageQueue($"Solution: {this._fileName} was successfully upgraded.", LogType.Info);

                }

                Thread.Sleep(5000);
            }
        }

        public D365Solution(
            string filePath,
            bool overwriteUnmanagedCustomziation,
            bool activatePlugins,
            bool replaceGuids,
            bool importAsHoldingSolution)
        {
            this._filePath = filePath;
            this._overwriteUnmanagedCustomization = overwriteUnmanagedCustomziation;
            this._activatePlugins = activatePlugins;
            this._replaceGuids = replaceGuids;
            this._importAsHoldingSolutions = importAsHoldingSolution;
        }

        private bool CheckSolutionInstallationInProgress()
        {
            string existingSolutionInstallation = @" 
                <fetch top='50' >
                  <entity name='asyncoperation' >
                    <attribute name='statecode' />
                    <attribute name='statuscode' />
                    <attribute name='name' />
                    <filter>
                      <condition attribute='name' operator='eq' value='ImportSolution' />
                      <condition attribute='completedon' operator='null' />
                      <filter type='or' >
                        <condition attribute='statuscode' operator='eq' value='0' />
                        <condition attribute='statuscode' operator='eq' value='10' />
                        <condition attribute='statuscode' operator='eq' value='20' />
                      </filter>
                    </filter>
                  </entity>
                </fetch>";

            EntityCollection asyncOperations = this._crmSvcClient.RetrieveMultiple(new FetchExpression(existingSolutionInstallation));

            if (asyncOperations != null && asyncOperations.Entities.Count > 0)
            {
                // Check for In-Progress async operation
                foreach (Entity asyncEntity in asyncOperations.Entities)
                {
                    if (asyncEntity.Contains("statuscode")
                        && asyncEntity.Attributes["statuscode"] != null
                        && ((OptionSetValue)asyncEntity.Attributes["statuscode"]).Value == 20)
                    {
                        QueryExpression qe = new QueryExpression();
                        qe.EntityName = "importjob";
                        qe.ColumnSet = new ColumnSet("operationcontext", "progress", "solutionname");

                        qe.Criteria = new FilterExpression();
                        qe.Criteria.AddCondition("createdon", ConditionOperator.LastXDays, 1);
                        qe.Criteria.AddCondition("completedon", ConditionOperator.Null);

                        qe.Orders.Add(new OrderExpression("createdon", OrderType.Descending));

                        EntityCollection importJobs = this._crmSvcClient.RetrieveMultiple(qe);

                        if (importJobs != null && importJobs.Entities != null && importJobs.Entities.Count > 0)
                        {
                            if (importJobs.Entities[0].Contains("solutionname")
                                && importJobs.Entities[0].Attributes["solutionname"] != null)
                            {
                                if (importJobs.Entities[0].Attributes["solutionname"].ToString() == this._uniqueName)
                                {

                                    this.MessageQueue($"Solution '{this._uniqueName}' installation is currently in progress. Retrieving the current import Jobid", LogType.Info);

                                    this._asyncOperationId = asyncEntity.Id;
                                    this._importJobId = importJobs.Entities[0].Id;

                                    return true;
                                }
                                else
                                {

                                    this.MessageQueue($"Existing solution '{importJobs.Entities[0].Attributes["solutionname"].ToString()}' installation is currently in progress. Please wait for the installation to complete...", LogType.Info);

                                    this.DisplaySolutionInstallProgress(asyncEntity.Id, importJobs.Entities[0].Id, importJobs.Entities[0].Attributes["solutionname"].ToString(), importJobs.Entities[0].Attributes["solutionname"].ToString());
                                }
                            }
                        }
                    }
                }

            }

            this._asyncOperationId = Guid.Empty;
            this._importJobId = Guid.Empty;

            return false;
        }

        private Entity GetAsyncOperationId(Guid operationId)
        {

            QueryExpression qe = new QueryExpression();
            qe.EntityName = "asyncoperation";
            qe.ColumnSet = new ColumnSet("statuscode", "statuscode", "completedon", "errorcode");

            qe.Criteria = new FilterExpression();
            qe.Criteria.AddCondition("asyncoperationid", ConditionOperator.Equal, operationId);

            EntityCollection col = this._crmSvcClient.RetrieveMultiple(qe);

            if (col != null && col.Entities.Count > 0)
            {
                return col.Entities[0];
            }

            return null;
        }

        private void DisplaySolutionInstallProgress(Guid asyncOperationId, Guid importId, string solutionName, string solutionUniqueName)
        {
            bool importCompleted = false;

            while (!importCompleted)
            {
                Entity asyncOperation = this.GetAsyncOperationId(asyncOperationId);

                if (asyncOperation != null)
                {
                    if (asyncOperation.Contains("statuscode")
                        && asyncOperation.Attributes["statuscode"] != null)
                    {
                        if (((OptionSetValue)asyncOperation.Attributes["statuscode"]).Value != 0
                        && ((OptionSetValue)asyncOperation.Attributes["statuscode"]).Value != 10)
                        {
                            QueryExpression qe = new QueryExpression();
                            qe.EntityName = "importjob";
                            qe.ColumnSet = new ColumnSet("operationcontext", "progress", "completedon");

                            qe.Criteria = new FilterExpression();
                            qe.Criteria.AddCondition("importjobid", ConditionOperator.Equal, importId);

                            EntityCollection col = this._crmSvcClient.RetrieveMultiple(qe);

                            if (col != null && col.Entities != null && col.Entities.Count > 0)
                            {
                                decimal progressIndicator;

                                if (Decimal.TryParse(col.Entities[0].Attributes["progress"].ToString(), out progressIndicator))
                                {
                                    if (progressIndicator == 100)
                                    {
                                        importCompleted = true;
                                        this.MessageQueue($"Solution ({solutionName}) was successfully installed.", LogType.Info);
                                    }
                                    else
                                    {
                                        if (asyncOperation.Contains("completedon") && asyncOperation.Attributes["completedon"] != null && asyncOperation.Attributes["completedon"].GetType() == typeof(System.DateTime))
                                        {
                                            throw new Exception($"Installation of Solution: {solutionName} has failed. Please check the 'Solution History' for more details");
                                        }
                                        this.MessageQueue($"Progress ({progressIndicator.ToString("0.00")}%)", LogType.InProgress);
                                    }
                                }
                            }
                            else
                            {
                                Guid jobId = this.RetrieveImportJobIdBySolutionName(solutionUniqueName);
                                if (jobId != Guid.Empty)
                                {
                                    importId = jobId;
                                }
                            }
                        }

                        if (!importCompleted
                            && asyncOperation.Contains("completedon")
                            && asyncOperation.Attributes["completedon"] != null
                            && asyncOperation.Attributes["completedon"].GetType() == typeof(System.DateTime))
                        {
                            if (((OptionSetValue)asyncOperation.Attributes["statuscode"]).Value == 31)
                            {
                                throw new Exception("Installation of Solution: " + solutionName + " has failed. Please check the 'Solution History' for more details");
                            }
                            else
                            {
                                importCompleted = true;

                                this.MessageQueue($"Solution: {solutionName} was successfully installed.", LogType.Info);
                            }
                        }
                    }
                }

                Thread.Sleep(5000);
            }
        }
        
        private Guid RetrieveImportJobIdBySolutionName(string solutionName)
        {
            QueryExpression importJob = new QueryExpression();
            importJob.EntityName = "importjob";
            importJob.ColumnSet = new ColumnSet("operationcontext", "progress");

            importJob.Criteria = new FilterExpression();
            importJob.Criteria.AddCondition("solutionname", ConditionOperator.Equal, solutionName);
            importJob.Criteria.AddCondition("progress", ConditionOperator.LessThan, "100");
            importJob.Criteria.AddCondition("createdon", ConditionOperator.LastXHours, 8);
            importJob.Criteria.AddCondition("completedon", ConditionOperator.Null);

            importJob.Orders.Add(new OrderExpression("createdon", OrderType.Descending));

            EntityCollection colImportJobs = this._crmSvcClient.RetrieveMultiple(importJob);

            if (colImportJobs != null && colImportJobs.Entities != null && colImportJobs.Entities.Count > 0)
            {

                this.MessageQueue($"Retrieving Import JobId for Solution: {solutionName}", LogType.Trace);

                return colImportJobs.Entities[0].Id;
            }

            return Guid.Empty;
        }

        private XmlDocument ReadSolutionXmlFile()
        {
            FileStream solutionZip = new FileStream(this._filePath, FileMode.Open);

            using (ZipArchive archive = new ZipArchive(solutionZip, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName == SOLUTION_XML_FILENAME)
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(XmlReader.Create(entry.Open()));

                        return doc;
                    }
                }
            }

            return null;
        }

        private void GetSolutionPackageDetails()
        {
            XmlDocument doc = this.ReadSolutionXmlFile();
            if (doc == null)
            {
                throw new Exception("Error occcurred while reading the solution xml file");
            }

            XmlNode nodes = doc.DocumentElement.SelectSingleNode("SolutionManifest/UniqueName");

            if (nodes != null)
            {
                this._uniqueName = doc.DocumentElement.SelectSingleNode("SolutionManifest/UniqueName").InnerText;
                this._versionNumber = doc.DocumentElement.SelectSingleNode("SolutionManifest/Version").InnerText;
                this._isManaged = (doc.DocumentElement.SelectSingleNode("SolutionManifest/Managed").InnerText == "1");
            }
        }
    }
}
