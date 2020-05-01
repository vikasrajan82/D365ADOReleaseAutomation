using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.EnableAccessTeam
{
    public class D365EnableAccess : TaskExtensionBase
    {
        private CrmServiceClient _crmServiceClient;

        public D365EnableAccess(string connectionString)
        {
            this._crmServiceClient = new CrmServiceClient(connectionString);
        }

        public void EnableAccessTeam(string entityList)
        {
            try
            {
                string[] entityNames = this.SplitToArray(entityList.Trim(), ',');

                bool entitiesEnabled = false;

                foreach (string entityName in entityNames)
                {
                    if (!string.IsNullOrWhiteSpace(entityName))
                    {
                        entitiesEnabled = true;

                        D365Entity entity = new D365Entity(entityName);

                        entity.MessageQueue += LogADOMessage;

                        entity.EnableAccessTeam(this._crmServiceClient);
                    }
                }

                if(!entitiesEnabled)
                {
                    this.LogADOMessage($"No entity is specified to enable access teams", LogType.Warning);
                }
            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);

                Environment.ExitCode = -1;
            }
        }

        private string[] SplitToArray(string delimitedString, char delimiter)
        {
            if (delimitedString.Contains(delimiter))
            {
                return delimitedString.Split(delimiter).ToArray();
            }
            else
            {
                return new string[] { delimitedString };
            }
        }
    }
}
