using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public static class ComConstants
    {  

        public const string BAUDRATECOM = "115200";
        public const string PARITYCOM = "None";
        public const int DATABITSCOM = 8;
        public const int STOPBITSCOM = 1;
        public const int PORT = 6666;
        public const string logFilepath = @"C:\POSLIBS";

        public const string createlogfile = "C:\\POSLIBS\\Configure.json";
    }
}
