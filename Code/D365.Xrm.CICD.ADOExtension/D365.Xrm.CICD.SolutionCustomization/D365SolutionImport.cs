using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace D365.Xrm.CICD.SolutionCustomization
{
    public class D365SolutionImport
    {
        private CrmServiceClient _crmServiceClient;

        private bool _showTraceMessages;

        private List<D365Solution> _lstSolutions = new List<D365Solution>();

        private string _configFilePath;

        public D365SolutionImport(string connectionString, bool displayTraceMessages)
        {
            this._crmServiceClient = new CrmServiceClient(connectionString);
            this._showTraceMessages = displayTraceMessages;
        }

        public void ProcessSolutionImport(string configFilePath)
        {
            try
            {
                if (this._crmServiceClient.ConnectedOrgId == null || this._crmServiceClient.ConnectedOrgId == Guid.Empty)
                {
                    throw new Exception("Error occurred while connecting to CDS. Please check the connection string");
                }

                this.LogADOMessage($"Connected to: {this._crmServiceClient.ConnectedOrgFriendlyName}", LogType.Info);

                this._configFilePath = configFilePath;

                this.IdentifySolutionsToBeInstalled();

                this.InstallSolutions();
            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);

                //this.LogADOMessage(ex.StackTrace, LogType.Debug);

                Environment.ExitCode = -1;
            }
        }

        private void LogADOMessage(string message, LogType logType)
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
                    Console.WriteLine($"       ===> {message}");
                    break;
                case LogType.Trace:
                    if (this._showTraceMessages)
                    {
                        Console.WriteLine(message);
                    }
                    break;

            }
        }

        private void InstallSolutions()
        {
            if (this._lstSolutions != null && _lstSolutions.Count > 0)
            {
                foreach (D365Solution solution in this._lstSolutions)
                {
                    solution.ValidateSolutionFile();

                    solution.ImportSolution(this._crmServiceClient);
                }
            }
        }

        private void IdentifySolutionsToBeInstalled()
        {
            this.LogADOMessage("Reading the configuration file to identify the solutions to be installed", LogType.Trace);

            XmlDocument configDoc = new XmlDocument();
            configDoc.Load(this._configFilePath);

            if (configDoc != null)
            {
                string directoryPath = Path.GetDirectoryName(this._configFilePath);

                XmlNode solutionNodes = configDoc.DocumentElement.SelectSingleNode("solutions");
                if (solutionNodes != null)
                {
                    foreach (XmlNode solutionFiles in solutionNodes.SelectNodes("solutionfile"))
                    {
                        var d365Solution = new D365Solution(
                                directoryPath + "\\Solutions\\" + solutionFiles.Attributes["solutionpackagefilename"].Value,
                                Convert.ToBoolean(solutionFiles.Attributes["overwriteunmanagedcustomizations"].Value),
                                Convert.ToBoolean(solutionFiles.Attributes["publishworkflowsandactivateplugins"].Value),
                                solutionFiles.Attributes["replacecanvasguids"] != null ? Convert.ToBoolean(solutionFiles.Attributes["replacecanvasguids"].Value) : false,
                                solutionFiles.Attributes["importasholdingsolution"] != null ? Convert.ToBoolean(solutionFiles.Attributes["importasholdingsolution"].Value) : false
                            );

                        d365Solution.MessageQueue += LogADOMessage;

                        this._lstSolutions.Add(d365Solution);
                    }
                }

                this.LogADOMessage("Solutions to be installed: " + this._lstSolutions.Count, LogType.Info);
            }
        }
    }
}
