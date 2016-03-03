using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HUwpBaseLib.Logs
{
    public class AzureTableLogEntity : TableEntity
    {
        public string HwId { get; set; }
        public string HwManufacturer { get; set; }
        public string HwProductName { get; set; }
        public string HwSku { get; set; }
        public string OsCat { get; set; }
        public string OsVersion { get; set; }
        public string OsArchitecture { get; set; }
        public string AppVersion { get; set; }
        public string UserId { get; set; }
        public string UserId2 { get; set; }
        public string UserId3 { get; set; }
        public string UserId4 { get; set; }
        public string UserId5 { get; set; }
        public string LogLevel { get; set; }
        public string Log { get; set; }
    }
}
