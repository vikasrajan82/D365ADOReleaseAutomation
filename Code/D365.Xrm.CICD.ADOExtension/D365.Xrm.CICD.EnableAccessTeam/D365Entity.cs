using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.EnableAccessTeam
{
    class D365Entity
    {
        private string _entityName;

        /// <summary>
        /// Log Message Handler
        /// </summary>
        /// <param name="e">Message Argument</param>
        public delegate void LogMessage(string msg, LogType logType);

        /// <summary>
        /// Log Message Event 
        /// </summary>
        public event LogMessage MessageQueue;

        public D365Entity(string entityName)
        {
            this._entityName = entityName.Trim();
        }

        private RetrieveEntityResponse RetrieveEntityMetadata(CrmServiceClient crmSvcClient)
        {
            try
            {
                RetrieveEntityRequest entityMetadataRequest = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Attributes,
                    LogicalName = this._entityName
                };

                return (RetrieveEntityResponse)crmSvcClient.Execute(entityMetadataRequest);
            }
            catch(Exception ex)
            {
                if (ex.Message.Contains("find the entity"))
                {
                    this.MessageQueue($"Entity with logical name '{this._entityName}' was not found.", LogType.TaskError);
                }
                else
                {
                    throw;
                }
            }

            return null;
        }

        public void EnableAccessTeam(CrmServiceClient crmSvcClient)
        {
            RetrieveEntityResponse entityMetadataResponse = this.RetrieveEntityMetadata(crmSvcClient);

            if (entityMetadataResponse != null)
            {
                EntityMetadata entityMetadata = entityMetadataResponse.EntityMetadata;
                if (entityMetadata.AutoCreateAccessTeams.HasValue && entityMetadata.AutoCreateAccessTeams.Value)
                {
                    this.MessageQueue($"Access Team is already enabled for entity '{this._entityName}'", LogType.Warning);
                    return;
                }

                entityMetadata.AutoCreateAccessTeams = true;
                UpdateEntityRequest updateEntityMetadata = new UpdateEntityRequest
                {
                    Entity = entityMetadata
                };

                this.MessageQueue($"Access Team is enabled for entity '{this._entityName}'", LogType.Info);

                crmSvcClient.Execute(updateEntityMetadata);
            }
        }
    }
}
