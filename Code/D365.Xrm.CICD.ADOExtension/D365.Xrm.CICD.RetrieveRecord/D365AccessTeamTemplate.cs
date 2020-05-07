using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.RetrieveRecord
{
    public class D365AccessTeamTemplate
    {
        [JsonProperty(PropertyName = "teamtemplateid")]
        internal Guid Id { get; set; }

        [JsonProperty(PropertyName = "teamtemplatename")]
        internal string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        internal string Description { get; set; }

        [JsonProperty(PropertyName = "defaultaccessrightsmask")]
        internal string DefaultAccessRightsMask { get; set; }

        [JsonProperty(PropertyName = "entityname")]
        internal string EntityName { get; set; }
    }
}
