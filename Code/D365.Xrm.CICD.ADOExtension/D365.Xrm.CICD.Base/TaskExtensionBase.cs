using System;
using System.Collections.Generic;
using System.Text;

namespace D365.Xrm.CICD.Base
{
    public class TaskExtensionBase
    {
        protected bool _showTraceMessages;

        protected void SetOutpuParameters(string variableName, string variableValue)
        {
            Console.WriteLine($"##vso[task.setvariable variable={variableName}]{variableValue}");
        }

        private string showInPorgressMessage(string message)
        {
            if (!string.IsNullOrEmpty(message) && message.Contains("===>"))
                return message;

            return $"       ===> {message}";
        }

        protected void LogADOMessage(string message, LogType logType)
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
                    Console.WriteLine(this.showInPorgressMessage(message));
                    break;
                case LogType.Trace:
                    if (this._showTraceMessages)
                    {
                        Console.WriteLine(message);
                    }
                    break;

            }
        }
    }
}
