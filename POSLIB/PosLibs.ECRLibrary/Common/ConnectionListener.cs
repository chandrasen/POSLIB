using PosLibs.ECRLibrary.Common.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit;

namespace PosLibs.ECRLibrary.Common
{
    public class ConnectionListener : IConnectionListener
    {

       public string errormessage = string.Empty;
        public string successmessage = string.Empty;

        public void OnFailure(string message)
        {
            errormessage = message;
        }

        public void OnSuccess(string message)
        {
            successmessage = message;
        }
    }
}
