using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public class EcrTcpipRequest
    {

        public EcrTcpipRequest() {
            cashierId = string.Empty;
            msgType = string.Empty;
            ecrIP = string.Empty;
            ecrPort = string.Empty;
            RFU1 = string.Empty;

        
        }
        public string cashierId { get; set; }
        public string msgType { get; set; }
        public string ecrIP { get; set; }
        public string ecrPort { get; set; }
        public string RFU1 { get; set; }

    }

   
}
