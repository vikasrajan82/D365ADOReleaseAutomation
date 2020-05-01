using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.PluginConfiguration
{
    public class D365SdkMessageStep
    {
        private CrmServiceClient _crmSvcClient;

        private EntityCollection _retrievedSdkMessages;

        private const string SDKMESSAGESTEPS_ENTITYNAME = "sdkmessageprocessingstep";

        private const string SDKMESSAGESTEPS_ATTR_NAME = "name";

        private const string SDKMESSAGESTEPS_ATTR_UNSECURECONFIGURATION = "configuration";

        private const string SDKMESSAGESTEPS_ATTR_SECURECONFIGURATIONID = "sdkmessageprocessingstepsecureconfigid";

        private const string SDKMESSAGESSECURECONFIGURATION_ENTITYNAME = "sdkmessageprocessingstepsecureconfig";

        private const string SDKMESSAGESSECURECONFIGURATION_ATTR_NAME = "secureconfig";

        private const string SDKMESSAGESSECURECONFIGURATION_ATTR_ALIAS = "pluginconfig";

        /// <summary>
        /// Log Message Handler
        /// </summary>
        /// <param name="e">Message Argument</param>
        public delegate void LogMessage(string msg, LogType logType);

        /// <summary>
        /// Log Message Event 
        /// </summary>
        public event LogMessage MessageQueue;

        public D365SdkMessageStep(
            CrmServiceClient crmSvcClient
            )
        {
            this._crmSvcClient = crmSvcClient;
        }

        private void RetrieveSdkMessage(List<KeyValuePair<string, string>> keyValuePairs, bool joinSecureEntity)
        {
            this._retrievedSdkMessages = null;

            QueryExpression query = new QueryExpression(SDKMESSAGESTEPS_ENTITYNAME);
            query.ColumnSet = new ColumnSet(SDKMESSAGESTEPS_ATTR_NAME,SDKMESSAGESTEPS_ATTR_UNSECURECONFIGURATION,SDKMESSAGESTEPS_ATTR_SECURECONFIGURATIONID);

            if (keyValuePairs != null)
            {
                FilterExpression filter = new FilterExpression(LogicalOperator.Or);
                foreach (KeyValuePair<string, string> sdkStepNameValue in keyValuePairs)
                {
                    filter.AddCondition(SDKMESSAGESTEPS_ATTR_NAME, ConditionOperator.Equal, sdkStepNameValue.Key);
                }
                query.Criteria.AddFilter(filter);
            }

            if (joinSecureEntity)
            {
                LinkEntity secureConfig = new LinkEntity(SDKMESSAGESTEPS_ENTITYNAME, SDKMESSAGESSECURECONFIGURATION_ENTITYNAME, SDKMESSAGESTEPS_ATTR_SECURECONFIGURATIONID, SDKMESSAGESTEPS_ATTR_SECURECONFIGURATIONID, JoinOperator.LeftOuter);
                secureConfig.EntityAlias = SDKMESSAGESSECURECONFIGURATION_ATTR_ALIAS;
                secureConfig.Columns = new ColumnSet(SDKMESSAGESSECURECONFIGURATION_ATTR_NAME);
                query.LinkEntities.Add(secureConfig);
            }

            this._retrievedSdkMessages = this._crmSvcClient.RetrieveMultiple(query);
        }

        private bool ValidateSdkMessagePresence(Entity sdkMessage, string sdkStepName)
        {
            if (sdkMessage == null)
            {
                this.MessageQueue($"Plugin Step with name '{sdkStepName}' was not found", LogType.TaskError);

                return false;
            }

            return true;
        }

        private bool VerifyExistingSdkSecureConfiguration(Entity sdkMessage, KeyValuePair<string, string> sdkStepNameValue)
        {
            if (sdkMessage.Contains(SDKMESSAGESTEPS_ATTR_SECURECONFIGURATIONID) 
                && sdkMessage.Attributes[SDKMESSAGESTEPS_ATTR_SECURECONFIGURATIONID] != null)
            {
                var secureConfigValue = (AliasedValue)sdkMessage[$"{SDKMESSAGESSECURECONFIGURATION_ATTR_ALIAS}.{SDKMESSAGESSECURECONFIGURATION_ATTR_NAME}"];

                if (secureConfigValue != null && secureConfigValue.Value.ToString() == sdkStepNameValue.Value)
                {
                    this.MessageQueue($"Secure configuration for Plugin step with name '{sdkStepNameValue.Key}' does not require any update.", LogType.Warning);
                }
                else
                {
                    Entity secureConfigurationRecord = new Entity(SDKMESSAGESSECURECONFIGURATION_ENTITYNAME, ((EntityReference)sdkMessage.Attributes[SDKMESSAGESTEPS_ATTR_SECURECONFIGURATIONID]).Id);
                    secureConfigurationRecord.Attributes[SDKMESSAGESSECURECONFIGURATION_ATTR_NAME] = sdkStepNameValue.Value;

                    this._crmSvcClient.Update(secureConfigurationRecord);

                    this.MessageQueue($"Secure configuration for Plugin step with name '{sdkStepNameValue.Key}' was successfully updated", LogType.Info);
                }

                return true;
            }

            return false;
        }


        private void UpdateExistingSdkUnsecureConfiguration(Entity sdkMessage, KeyValuePair<string, string> sdkStepNameValue)
        {
            if (sdkMessage.Contains(SDKMESSAGESTEPS_ATTR_UNSECURECONFIGURATION)
                && sdkMessage.Attributes[SDKMESSAGESTEPS_ATTR_UNSECURECONFIGURATION] != null)
            {
                var unsecureConfigValue = sdkMessage.Attributes[SDKMESSAGESTEPS_ATTR_UNSECURECONFIGURATION].ToString();

                if (!string.IsNullOrEmpty(unsecureConfigValue) && unsecureConfigValue == sdkStepNameValue.Value)
                {
                    this.MessageQueue($"Unsecure configuration for plugin step with name '{sdkStepNameValue.Key}' does not require any update.", LogType.Warning);

                    return;
                }
            }

            Entity unsecureConfigurationRecord = new Entity(SDKMESSAGESTEPS_ENTITYNAME, sdkMessage.Id);

            unsecureConfigurationRecord.Attributes[SDKMESSAGESTEPS_ATTR_UNSECURECONFIGURATION] = sdkStepNameValue.Value;

            this._crmSvcClient.Update(unsecureConfigurationRecord);

            this.MessageQueue($"Unsecure configuration for plugin step with name '{sdkStepNameValue.Key}' was successfully updated", LogType.Info);
        }

        private void CreateSecureConfiguration(Entity sdkMessage, KeyValuePair<string, string> sdkStepNameValue)
        {
            Entity secureConfigurationRecord = new Entity(SDKMESSAGESSECURECONFIGURATION_ENTITYNAME);
            secureConfigurationRecord.Attributes[SDKMESSAGESSECURECONFIGURATION_ATTR_NAME] = sdkStepNameValue.Value;

            var sdkSecureConfigurationRecord = this._crmSvcClient.Create(secureConfigurationRecord);

            Entity sdkStepMessage = new Entity(SDKMESSAGESTEPS_ENTITYNAME, sdkMessage.Id);
            sdkStepMessage.Attributes[SDKMESSAGESTEPS_ATTR_SECURECONFIGURATIONID] = new EntityReference(SDKMESSAGESSECURECONFIGURATION_ENTITYNAME, sdkSecureConfigurationRecord);

            this._crmSvcClient.Update(sdkStepMessage);

            this.MessageQueue($"Secure configuration for Plugin step with name '{sdkStepNameValue.Key}' was successfully updated", LogType.Info);
        }

        public void UpdateSecureConfiguration(List<KeyValuePair<string, string>> keyValuePairs)
        {
            this.RetrieveSdkMessage(keyValuePairs ,true);

            foreach (KeyValuePair<string, string> sdkStepNameValue in keyValuePairs)
            {
                Entity sdkMessage = this.GetSdkMessageRecord(sdkStepNameValue.Key);

                if (this.ValidateSdkMessagePresence(sdkMessage, sdkStepNameValue.Key)
                    && !this.VerifyExistingSdkSecureConfiguration(sdkMessage, sdkStepNameValue))
                {
                    this.CreateSecureConfiguration(sdkMessage, sdkStepNameValue);
                }
            }
        }

        public void UpdateUnsecureConfiguration(List<KeyValuePair<string, string>> keyValuePairs)
        {
            this.RetrieveSdkMessage(keyValuePairs, false);

            foreach (KeyValuePair<string, string> sdkStepNameValue in keyValuePairs)
            {
                Entity sdkMessage = this.GetSdkMessageRecord(sdkStepNameValue.Key);

                if (this.ValidateSdkMessagePresence(sdkMessage, sdkStepNameValue.Key))
                {
                    this.UpdateExistingSdkUnsecureConfiguration(sdkMessage, sdkStepNameValue);
                }
            }
        }

        private Entity GetSdkMessageRecord(string sdkStepName)
        {
            if (this._retrievedSdkMessages != null && this._retrievedSdkMessages.Entities.Count > 0)
            {
                return this._retrievedSdkMessages.Entities.Where(x => x.Contains(SDKMESSAGESTEPS_ATTR_NAME) && (string)x.GetAttributeValue<string>(SDKMESSAGESTEPS_ATTR_NAME) == sdkStepName).FirstOrDefault();
            }

            return null;
        }

    }
}
