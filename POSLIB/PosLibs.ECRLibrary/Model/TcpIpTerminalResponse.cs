using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public class TcpIpTerminalResponse
    {
        public TcpIpTerminalResponse() { }

        public string dateTime { get; set; }

        public string isWifiSupported { get; set; }
        public string protocolType { get; set; }
        public string posControllerId { get; set; }
        public string posIP { get; set; }
        public string posPort { get; set; }
        public int transactionType { get; set; }
    }
}
