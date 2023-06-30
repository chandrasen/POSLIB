using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSController.Model
{
    public class PosData
    {
        public string POSControllerId { get; set; }
        public string DateAndTime { get; set; }
        public string TransactionID { get; set; }
        public int TransactionType { get; set; }
        public int ProtocolType { get; set; }
        public string ECRTCPIP { get; set; }
        public int ECRTCPPort { get; set; }
        public string RFU1 { get; set; }
    }
}
