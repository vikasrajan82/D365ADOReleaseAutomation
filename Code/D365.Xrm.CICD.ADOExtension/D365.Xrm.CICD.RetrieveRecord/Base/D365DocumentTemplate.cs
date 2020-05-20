using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.RetrieveRecord
{
    internal class D365DocumentTemplate
    {
        private Entity _docTemplate;

        /// <summary>
        /// Log Message Handler
        /// </summary>
        /// <param name="e">Message Argument</param>
        public delegate void LogMessage(string msg, LogType logType);

        /// <summary>
        /// Log Message Event 
        /// </summary>
        public event LogMessage MessageQueue;

        internal bool HasContent
        {
            get
            {
                return (this._docTemplate.Contains("content")
                    && this._docTemplate.Attributes["content"] != null);
            }
        }

        internal string Name
        {
            get
            {
                if(this._docTemplate.Contains("name")
                    && this._docTemplate.Attributes["name"] != null)
                {
                    return this._docTemplate.Attributes["name"].ToString();
                }

                return string.Empty;
            }
        }

        internal int DocumentType
        {
            get
            {
                if (this._docTemplate.Contains("documenttype")
                    && this._docTemplate.Attributes["documenttype"] != null)
                {
                    return ((OptionSetValue)this._docTemplate.Attributes["documenttype"]).Value;
                }

                return -1;
            }
        }

        internal string DocumentEntityName
        {
            get
            {
                if (this._docTemplate.Contains("associatedentitytypecode")
                    && this._docTemplate.Attributes["associatedentitytypecode"] != null)
                {
                    return this._docTemplate.Attributes["associatedentitytypecode"].ToString();
                }

                return string.Empty;
            }
        }


        internal D365DocumentTemplate(Entity documentTemplate)
        {
            this._docTemplate = documentTemplate;

            if(this._docTemplate ==null)
            {
                throw new Exception("DocumentTemplate record cannot be null");
            }
        }

        internal dynamic ProcessDocument(string templateFolder, int entityTypeCode)
        {
            if (!this.HasContent)
            {
                this.MessageQueue($"There is no document associated with template ({this.Name})", LogType.Error);

                return null;
            }

            string _fileName = this.SaveDocument(templateFolder);

            dynamic _documentTemplate = new System.Dynamic.ExpandoObject();
            _documentTemplate.documenttemplateid = this._docTemplate.Id;
            _documentTemplate.documenttype = this.DocumentType;
            _documentTemplate.name = this.Name;
            _documentTemplate.filename = _fileName;
            _documentTemplate.associatedentitytypecode = entityTypeCode;
            _documentTemplate.entityname = this.DocumentEntityName;

            return _documentTemplate;
        }

        private string SaveDocument(string templateFolder)
        {
            string _fileName = Regex.Replace(this.Name, @"[^A-Za-z]+", String.Empty) + "_" + DateTime.Now.ToString("HHmmssfff") + (this.DocumentType == 1 ? ".xlsx" : ".docx");

            using (FileStream fileStream = new FileStream(templateFolder + "\\" + _fileName, FileMode.OpenOrCreate))
            {
                Byte[] bytes = Convert.FromBase64String(this._docTemplate.Attributes["content"].ToString());
                fileStream.Write(bytes, 0, bytes.Length);
            }

            return _fileName;
        }
    }
}
