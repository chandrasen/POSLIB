using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public class ComDeviceRequest
    {
        public ComDeviceRequest() { }
        public string posControllerId { get; set; }
        public string transactionType { get; set; }
        public string dateTime { get; set; }
        public string RFU1 { get; set; }

       


    }
}
