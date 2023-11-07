using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public class TCPIPHealthCheck
    {
        public TCPIPHealthCheck() {
            cashierid = string.Empty;
            msgType = string.Empty;
            pType = string.Empty;
        }

        public string cashierid { get; set; }
        public  string msgType { get; set; }
        public  string pType { get; set; }
    }
}
