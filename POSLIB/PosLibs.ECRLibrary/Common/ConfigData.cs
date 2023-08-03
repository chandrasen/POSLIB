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
            tcpIp=string.Empty;
            connectionMode = string.Empty;
            CashierID = string.Empty;
            CashierName = string.Empty;
        }
        public int commPortNumber { get; set; }
        public string tcpIp { get; set; }
        public int tcpPort { get; set; }
        public string  connectionMode { get; set; }
        public string[] communicationPriorityList { get; set; }
        public bool isConnectivityFallBackAllowed { get; set; }
        public string CashierID { get; set; }   
        public string CashierName { get; set; }

    }
}
