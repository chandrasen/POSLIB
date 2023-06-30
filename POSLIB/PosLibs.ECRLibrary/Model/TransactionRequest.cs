using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public  class TransactionRequest
    {
        public TransactionRequest() { }
        public string cashierId { get; set; }

        public string msgType { get; set; }
        public string ptype { get; set; }

        public string requestBody { get; set; }
        public string RFU1 { get; set; }
    }



    
}
