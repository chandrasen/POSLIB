using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public  class TransactionReponse
    {
       public TransactionReponse() {

            responseBody = string.Empty;
            cashierID = string.Empty;

        }
        public string responseBody { get; set; }
        public string cashierID { get; set; }
        public bool isDemoMode { get; set; }
        public int msgType { get; set; }
        public int pType { get; set; }
    }
}
