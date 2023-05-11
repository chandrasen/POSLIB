using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public class ComTerminalResponse
    {

        public ComTerminalResponse() { }
        public string TID { get; set; }
        public Boolean isCommSupported { get; set; }
        public string MerchantName { get; set; }
        public string posControllerId { get; set; }
        public string protocolType { get; set; }
        public string transactionType { get; set; }


        public  string COM { get; set; }
    }
}
