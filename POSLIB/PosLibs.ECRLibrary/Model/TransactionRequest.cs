using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public  class TransactionRequest
    {
        public TransactionRequest() {

            cashierId = string.Empty;
            requestBody = string.Empty;
        }
        public string cashierId { get; set; }
        public Boolean isDemoMode { get; set; }
        public int msgType { get; set; }
        public int pType { get; set; }
        public string requestBody { get; set; }
       
    }
}
