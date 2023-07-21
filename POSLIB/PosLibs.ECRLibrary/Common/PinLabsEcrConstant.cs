using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public class PinLabsEcrConstant
    {
        PinLabsEcrConstant() { }

        public const string TCPIP = "TCP/IP";
        public const string COM = "COM";
        public const string FILE_NOT_FOUND = "File Not Found Exception";
        public const string TXN_TME_OUT_MSG = "Transcation Time out";
        public const string NO_DEVICE_FOUND = "No Device Found";
        public const string TIME_OUT_EXC = "Time Out Exception";
        public const string IO_EXC_MSG = "IOException";

        public const int SOCK_EXCEPTION = 1001;
        public const int NO_DEV_FOUND = 1002;
        public const int TIME_OUT_EXCEPTION = 1003;
        public const int CON_FAILD_EXCEPTION = 1004;
        public const int TXN_FAILD = 1005;
        public const int IOEXCEPTION = 1006;

        public const string FILE_PATH = "C:\\PinLabs\\Configure.json";











    }
}
