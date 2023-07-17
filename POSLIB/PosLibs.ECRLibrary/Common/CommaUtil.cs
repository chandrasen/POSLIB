using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PosLibs.ECRLibrary.Model;
namespace PosLibs.ECRLibrary.Common
{
    public class CommaUtil
    {
        protected CommaUtil()
        {
            
        }
        public static bool CheckIPAddress(string ipAddress)
        {
            try
            {
                if (!IPAddress.TryParse(ipAddress, out IPAddress? parsedIPAddress) || parsedIPAddress == null)
                    return false;
                string[] octets = ipAddress.Split('.');
                if (octets.Length != 4)
                    return false;
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid IP Exception");
            }
            return true;
        }
    }
}
