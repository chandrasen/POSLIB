using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public class ComDeviceRequest
    {
        public ComDeviceRequest() 
        {
            cashierId = string.Empty;
            msgType = string.Empty;
            RFU1 = string.Empty;
        }
        public string cashierId { get; set; }
        public  string msgType { get; set; }
        public  string RFU1 { get; set; }
    }
}
