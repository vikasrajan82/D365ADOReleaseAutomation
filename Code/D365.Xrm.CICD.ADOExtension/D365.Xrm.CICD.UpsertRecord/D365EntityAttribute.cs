using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.UpsertRecord
{
    public class D365EntityAttribute
    {
        [JsonProperty("name")]
        public string LogicalName { get; set; }

        [JsonProperty("value")]
        public string AttributeValue { get; set; }

        [JsonProperty("lookupentity")]
        public string LookupEntityName { get; set; }

        private object _newAttributeValue;

        private object _retrievedAttributeValue;

        private AttributeTypeCode _attributeType;

        public object NewAttributeValue
        {
            get
            {
                return this._newAttributeValue;
            }
        }

        public void MarkAttributeAvailable(AttributeTypeCode typeCode, string[] lookupTargets)
        {
            this._attributeType = typeCode;

            if(lookupTargets !=null && lookupTargets.Length == 1 )
            {
                this.LookupEntityName = lookupTargets[0];
            }

            try
            {
                this._newAttributeValue = this.GetConvertedValue(this.AttributeValue);
            }
            catch(Exception ex)
            {
                throw new Exception($"Error occured while reading the values for attribute {this.LogicalName}. Detailed Log: {ex.Message}");
            }
        }

        public void SetRetrievedValue(object retrievedValue)
        {
            this._retrievedAttributeValue = retrievedValue;
        }

        public bool isAttributeValuesDifferent
        {
            get
            {
                if (this._retrievedAttributeValue == null && this._newAttributeValue == null)
                    return false;

                if (this._retrievedAttributeValue == null || this._newAttributeValue == null)
                    return true;

                return this.CompareAttributePreviousCurrentValue();
            }
        }

        private object GetConvertedValue(object attributeValue)
        {
            if (attributeValue == null)
            {
                return null;
            }

            switch (this._attributeType)
            {
                case AttributeTypeCode.BigInt:
                    return Convert.ToInt64(attributeValue);
                case AttributeTypeCode.String:
                case AttributeTypeCode.Memo:
                    return Convert.ToString(attributeValue);
                case AttributeTypeCode.Boolean:
                    return Convert.ToBoolean(attributeValue);
                case AttributeTypeCode.DateTime:
                    return Convert.ToDateTime(attributeValue);
                case AttributeTypeCode.Decimal:
                    return Convert.ToDecimal(attributeValue);
                case AttributeTypeCode.Double:
                    return Convert.ToDouble(attributeValue);
                case AttributeTypeCode.Integer:
                    return Convert.ToInt32(attributeValue);
                case AttributeTypeCode.Lookup:
                    if(string.IsNullOrEmpty(this.LookupEntityName))
                    {
                        throw new Exception($"The loopup entity name is missing for the attribute {this.LogicalName}. Please specify the same in the input json.");
                    }
                    return new EntityReference(this.LookupEntityName, new Guid(attributeValue.ToString()));
                case AttributeTypeCode.Money:
                    return (Money)attributeValue;
                case AttributeTypeCode.Picklist:
                    Int32 optionSetCode;
                    if (!Int32.TryParse(attributeValue.ToString(), out optionSetCode))
                    {
                        throw new Exception($"Attribute '{this.LogicalName}' has incorrect value specified. Please ensure that the option set code exists in the target environment.");
                    }
                    return new OptionSetValue(optionSetCode);
                case AttributeTypeCode.Uniqueidentifier:
                    return new Guid(attributeValue.ToString());
                default:
                    throw new Exception($"Data of type {this._attributeType.ToString()} is not yet supported.");
            }
        }

        private bool CompareAttributePreviousCurrentValue()
        {
            switch (this._attributeType)
            {
                case AttributeTypeCode.BigInt:
                    return Convert.ToInt64(this._retrievedAttributeValue) != Convert.ToInt64(this._newAttributeValue);
                case AttributeTypeCode.String:
                case AttributeTypeCode.Memo:
                    return this._retrievedAttributeValue.ToString() != this._newAttributeValue.ToString();
                case AttributeTypeCode.Boolean:
                    return Convert.ToBoolean(this._retrievedAttributeValue) != Convert.ToBoolean(this._newAttributeValue);
                case AttributeTypeCode.DateTime:
                    return Convert.ToDateTime(this._retrievedAttributeValue) != Convert.ToDateTime(this._newAttributeValue);
                case AttributeTypeCode.Decimal:
                    return Convert.ToDecimal(this._retrievedAttributeValue) != Convert.ToDecimal(this._newAttributeValue);
                case AttributeTypeCode.Double:
                    return Convert.ToDouble(this._retrievedAttributeValue) != Convert.ToDouble(this._newAttributeValue);
                case AttributeTypeCode.Integer:
                    return Convert.ToInt32(this._retrievedAttributeValue) != Convert.ToInt32(this._newAttributeValue);
                case AttributeTypeCode.Lookup:
                    return ((EntityReference)this._retrievedAttributeValue).Id != ((EntityReference)this._newAttributeValue).Id;
                case AttributeTypeCode.Money:
                    return ((Money)this._retrievedAttributeValue).Value != ((Money)this._newAttributeValue).Value;
                case AttributeTypeCode.Picklist:
                    return ((OptionSetValue)this._retrievedAttributeValue).Value != ((OptionSetValue)this._newAttributeValue).Value;
                case AttributeTypeCode.Uniqueidentifier:
                    return (Guid)this._retrievedAttributeValue != (Guid)this._newAttributeValue;
                default:
                    throw new Exception($"Data of type {this._attributeType.ToString()} is not yet supported.");
            }
        }
    }
}
