using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.PluginConfiguration
{
    public class D365UpdatePluginConfiguration : TaskExtensionBase
    {
        private CrmServiceClient _crmServiceClient;

        private Dictionary<int, List<KeyValuePair<string, string>>> _stepNameValuePair;

        private const int ARRAY_COUNT = 10;

        public D365UpdatePluginConfiguration(string connectionString)
        {
            this._crmServiceClient = new CrmServiceClient(connectionString);

            this._stepNameValuePair = new Dictionary<int, List<KeyValuePair<string, string>>>();
        }

        private void DeserializeJsonString(string stepNameValueJson)
        {
            try
            {
                Dictionary<string, string> stepNameValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(stepNameValueJson);

                int stepIndex = 1, internalCounter = 1;
                foreach (KeyValuePair<string, string> nameValuePair in stepNameValuePairs)
                {
                    if (!this._stepNameValuePair.ContainsKey(stepIndex))
                    {
                        this._stepNameValuePair.Add(stepIndex, new List<KeyValuePair<string, string>>());
                    }

                    this._stepNameValuePair[stepIndex].Add(nameValuePair);

                    internalCounter++;

                    if (internalCounter > ARRAY_COUNT)
                    {
                        stepIndex++;
                        internalCounter = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while reading the name/value json: {ex.Message}");
            }
        }

        public void ProcessPluginSecureConfigUpdate(string stepNameValueJson)
        {
            this.ProcessPluginConfigUpdate(PluginConfigurationType.Secure, stepNameValueJson);
        }

        public void ProcessPluginUnsecureConfigUpdate(string stepNameValueJson)
        {
            this.ProcessPluginConfigUpdate(PluginConfigurationType.Unsecure, stepNameValueJson);
        }

        private void ProcessPluginConfigUpdate(
            PluginConfigurationType configType,
            string stepNameValueJson)
        {
            try
            {
                this.DeserializeJsonString(stepNameValueJson);

                D365SdkMessageStep sdkMessage = new D365SdkMessageStep(this._crmServiceClient);

                sdkMessage.MessageQueue += LogADOMessage;
                
                switch (configType)
                {
                    case PluginConfigurationType.Secure:
                        foreach (var stepNameValuePair in this._stepNameValuePair)
                        {
                            sdkMessage.UpdateSecureConfiguration(stepNameValuePair.Value);
                        }
                        break;
                    case PluginConfigurationType.Unsecure:
                        foreach (var stepNameValuePair in this._stepNameValuePair)
                        {
                            sdkMessage.UpdateUnsecureConfiguration(stepNameValuePair.Value);
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                this.LogADOMessage(ex.Message, LogType.TaskError);

                Environment.ExitCode = -1;
            }
        }
    }
}
