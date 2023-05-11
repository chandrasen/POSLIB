﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public  class TransactionRequest
    {
        public TransactionRequest() { }
        public string posControllerId { get; set; }
        public string dateTime { get; set; }
        public string transactionID { get; set; }
        public string transactionType { get; set; }
        public string protocalType { get; set; }
        public string requestBody { get; set; }
        public string RFU1 { get; set; }

    }



    
}
