using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.UpsertRecord
{
    public static class AttributeMetadataExtension
    {
        public static AttributeMetadata FindAttribute(this AttributeMetadata[] attrMetadata, string attributeName )
        {
            foreach(AttributeMetadata attr in attrMetadata)
            {
                if (attr.LogicalName == attributeName)
                {
                    return attr;
                }
            }

            return null;
        }
    }
}
