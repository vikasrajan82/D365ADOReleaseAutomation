using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.Base
{
    public enum LogType
    {
        Warning,
        Debug,
        Error,
        TaskError,
        Info,
        Trace,
        InProgress
    }

    public enum RetrieveRecordBy
    {
        GUID,
        FetchXML
    }

    public enum PluginConfigurationType
    {
        Secure,
        Unsecure
    }
}
