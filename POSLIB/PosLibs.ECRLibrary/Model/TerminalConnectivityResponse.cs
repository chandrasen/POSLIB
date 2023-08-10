using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public class TerminalConnectivityResponse
    {

        public TerminalConnectivityResponse() {
            devId = string.Empty;
            MerchantName = string.Empty;
            cashierId = string.Empty;
            protocolType = string.Empty;
            transactionType = string.Empty;
            dateTime = string.Empty;
            slNo = string.Empty;
            isWifiSupported = string.Empty;
            posIP = string.Empty;
            posPort = string.Empty;
            COM = string.Empty;
        }
        public string devId { get; set; }
        public Boolean isCommSupported { get; set; }
        public string MerchantName { get; set; }
        public string cashierId { get; set; }
        public string protocolType { get; set; }
        public string transactionType { get; set; }
        public string dateTime { get; set; }

        public int msgType { get; set; }

        public string slNo { get; set; }

        public string isWifiSupported { get; set; }
       
        public string posIP { get; set; }
        public string posPort { get; set; }
       


        public  string COM { get; set; }
    }
}
