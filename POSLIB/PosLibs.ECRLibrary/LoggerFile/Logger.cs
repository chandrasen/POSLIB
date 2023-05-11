using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.LoggerFile
{
  public static class Logger
    {
        public static readonly string LogFilePath = "log.txt";
        private static bool isLoggingEnabled;
        public static void Log(string message)
        {
            if (isLoggingEnabled)
            {
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
        }
    }
}
