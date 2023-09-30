using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public class PosLibConstant
    {
        PosLibConstant() { }

        public const string TCPIP = "TCP/IP";
        public const string COM = "COM";
        public const string FILE_NOT_FOUND = "File Not Found Exception";
        public const string TXN_TME_OUT_MSG = "Transcation Time out";
        public const string NO_DEVICE_FOUND = "No Device Found";
        public const string TIME_OUT_EXC = "Time Out Exception";
        public const string IO_EXC_MSG = "IOException";
        public const string CONNECTIONFAIELD = "TCP/IP Connection Failed Please check conneciton";
        public const string GENERAL_EXCEPTION ="1008";
        public const string COMFAIELD_ERROR = "COM Connection Failed";
        public const string TCPIPFAIELD_ERROR = "TCP IP Connection Failed";
        public const string TCPIPFAIELD_TXN_ERROR = "Connection failed";
        public const string COMFAIELD_TXN_ERROR = "Transaction Failed Due COM not connected";
        public const string TCPIPCONNECTION_SUCCESS = "TCP/IP Connected Successfully";
        public const string COMCONNECTION_SUCCESS = "COM Connected Successfully";

        public const int SOCK_EXCEPTION = 1001;
        public const int NO_DEV_FOUND = 1002;
        public const int TIME_OUT_EXCEPTION = 1003;
        public const int CON_FAILD_EXCEPTION = 1004;
        public const int AUTOFALLBACK_TXN_FAIELD = 1009;
        public const string AUTOFALLBACK_TXN_FAIELD_MSG ="Auto fallback Transaction Failed";
        public const int TXN_FAILD = 1005;
        public const int IOEXCEPTION = 1006;
        public const string CONNECTIONFAIELDCODE = "4005";

        public const string FILE_PATH = "C:\\POSLIBS\\";
        public const string FILE_NAME = $"Configure.json";


        public const string COMHEALTHACTIVE = "100";
        public const string COMHEALTHINACTIVE = "200";
        public const string TCPIPHEALTHACTIVE = "400";
        public const string TCPIPHEALTHINACTIVE = "300";











    }
}
