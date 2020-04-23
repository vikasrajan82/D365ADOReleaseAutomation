using D365.Xrm.CICD.Base;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace D365.Xrm.CICD.DataImport
{
    class D365DataZip
    {
        private string _zipFilePath;

        private string _zipFileNameWithoutExtension;

        private bool _enableBatchMode;

        private int _batchSize;

        private int _concurrentThreads;

        /// <summary>
        /// Log Message Handler
        /// </summary>
        /// <param name="e">Message Argument</param>
        public delegate void LogMessage(string msg, LogType logType);

        /// <summary>
        /// Log Message Event 
        /// </summary>
        public event LogMessage MessageQueue;

        public D365DataZip(
            string filePath,
            bool enableBatchMode,
            int batchSize,
            int concurrentThreads
            )
        {
            this._zipFilePath = filePath;
            this._enableBatchMode = enableBatchMode;
            if(this._enableBatchMode)
            {
                this._batchSize = batchSize;
            }

            this._concurrentThreads = concurrentThreads;

            if(File.Exists(this._zipFilePath))
            {
                this._zipFileNameWithoutExtension = Path.GetFileNameWithoutExtension(this._zipFilePath);
            }
            else
            {
                throw new Exception($"The data zip file '{filePath}' does not exist");
            }
        }

        public void ImportMasterData(Dictionary<string, string> guidsEntry, Runspace psRunSpace, string connectionString)
        {
            this.UpdateMasterEntitiesGuidData(guidsEntry);

            this.UploadMasterDataEntities(psRunSpace, connectionString);
        }

        private void UpdateMasterEntitiesGuidData(Dictionary<string, string> guidsEntry)
        {
            if (guidsEntry == null || guidsEntry.Count == 0)
                return;

            StringBuilder str;

            FileStream datazip = new FileStream(this._zipFilePath, FileMode.Open);
            using (ZipArchive archive = new ZipArchive(datazip, ZipArchiveMode.Update))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName == @"data.xml")
                    {
                        using (TextReader reader = new StreamReader(entry.Open()))
                        {
                            str = new StringBuilder(reader.ReadToEnd());
                            foreach (KeyValuePair<string, string> item in guidsEntry)
                            {
                                if (!string.IsNullOrWhiteSpace(item.Value))
                                {
                                    str = str.Replace(item.Key, item.Value);
                                }
                            }
                        }

                        using (TextWriter writer = new StreamWriter(entry.Open()))
                        {
                            writer.Write(str);
                        }
                    }
                }
            }
        }

        private void UploadMasterDataEntities(Runspace psRunSpace, string connectionString)
        {
            this.MessageQueue($"Processing {Path.GetFileName(this._zipFilePath)}", LogType.Info);

            using (PowerShell ps = PowerShell.Create())
            {
                ps.Runspace = psRunSpace;

                ps.AddCommand("Import-CrmDataFile")
                  .AddParameter("DataFile", this._zipFilePath)
                  .AddParameter("CrmConnection", connectionString);

                if (this._enableBatchMode)
                {
                    ps.AddParameter("EnabledBatchMode")
                      .AddParameter("BatchSize", this._batchSize);
                }

                if (this._concurrentThreads > 0)
                {
                    ps.AddParameter("ConcurrentThreads", this._concurrentThreads);
                }
                ////.AddParameter("LogWriteDirectory", @"C:\Program Files\WindowsPowerShell\Modules\Microsoft.Xrm.Tooling.ConfigurationMigration\1.0.0.26");

                ps.Streams.Progress.DataAdded += Progress_DataAdded;

                ps.Invoke();

                if (ps.Streams.Error.Count > 0)
                {
                    foreach (ErrorRecord error in ps.Streams.Error)
                    {
                        this.MessageQueue(error.ToString(), LogType.Warning);
                    }
                }
            }
        }

        private void Progress_DataAdded(object sender, DataAddedEventArgs e)
        {
            ProgressRecord newRecord = ((PSDataCollection<ProgressRecord>)sender)[e.Index];
            if (!string.IsNullOrEmpty(newRecord.CurrentOperation))
            {
                this.MessageQueue(newRecord.CurrentOperation, LogType.InProgress);
            }
        }
    }
}
