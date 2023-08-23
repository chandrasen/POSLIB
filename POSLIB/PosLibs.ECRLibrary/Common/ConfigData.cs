using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public class ConfigData
    {
        public ConfigData()
        {
            tcpIp = string.Empty;
            connectionMode = string.Empty;
            CashierID = string.Empty;
            CashierName = string.Empty;
            comfullName = string.Empty;
            comserialNumber = string.Empty;
            tcpIpaddress = string.Empty;
            retrivalcount = string.Empty;
            connectionTimeOut = string.Empty;
            tcpIpPort = string.Empty;
        }
        public int commPortNumber { get; set; }
        public string tcpIp { get; set; }
        public int tcpPort { get; set; }
        public string connectionMode { get; set; }
        public string[] communicationPriorityList { get; set; }
        public bool isConnectivityFallBackAllowed { get; set; }
        public string CashierID { get; set; }
        public string CashierName { get; set; }
        public string retrivalcount { get; set; }
        public string connectionTimeOut { get; set; }
        public string comfullName { get; set; }
        public string comserialNumber { get; set; }
        public string tcpIpaddress { get; set; }
        public string tcpIpPort { get; set; }
        public string tcpIpDeviceId { get; set; }

        public string tcpIpSerialNumber { get; set; }
        public string comDeviceId { get; set; }

        public string LogPath { get; set; }

        public string loglevel { get; set; }

    }
}
