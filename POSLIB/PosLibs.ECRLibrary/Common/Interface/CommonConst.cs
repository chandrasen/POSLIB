using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common.Interface
{
    public class CommonConst
    {
        protected CommonConst()
        {

        }
        public const string cashierID = "12345678";
        public const string cashierid = "222";
        public const string cashierId = "123456";
        public const string msgType1_1 = "1";
        public const string msgType1_2 = "2";
        public const string msgType1_7 = "7";
        public const string pType = "1";
        public const string ecrPort = "6666";
        public const string ManagementObjectSearcher = "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'";
    }
}
