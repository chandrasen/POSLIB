using PosLibs.ECRLibrary.Common.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public class ComEventListenercls:ComEventListener
    {
       public ComEventListenercls()
        {
            errormessage = string.Empty;
            successmessage = string.Empty;
            successcodemsg = string.Empty;
            errorcodemsg = string.Empty;
        }
        public string errormessage { get; set; }
        public string successmessage { get; set; }
        public string errorcodemsg { get; set; }
        public string successcodemsg { get; set; }

        public void OnFailure(string errorcode)
        {
            errorcodemsg = errorcode;
        }

        public void OnSuccess(string errorcode)
        {
            successcodemsg = errorcode;
        }
    }
}
