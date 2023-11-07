using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public class ComHealthCheckRequest
    {
        public ComHealthCheckRequest()
        {
            cashierId = string.Empty;
            msgType = string.Empty;
            pType = string.Empty;
        }

        public string cashierId { get; set; }
        public string msgType { get; set; }
        public string pType { get; set; }
    }
}
