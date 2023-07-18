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
        public static string stringToCsv(int txntype,string amount)
        {
            string value = "TX12345678";
            string fullrequbody = txntype.ToString() + "," + value + "," + amount + "," + "," + "," + "," + "," + "," + ",";
          string HaxDecimalreqbody=  GetPaymentPacket(fullrequbody);
            return HaxDecimalreqbody;
        }
        static string GetPaymentPacket(string csvData)
        {
            if (!string.IsNullOrEmpty(csvData))
            {
                int iOffset = 0;
                byte[] msgBytes = Encoding.UTF8.GetBytes(csvData);
                int iCSVLen = msgBytes.Length;
                int finalMsgLen = iCSVLen + 7;
                // 7 = 2 byte source , 2 byte function code, 2 byte length, 1 byte termination
                byte[] msgBytesExtra = new byte[finalMsgLen];
                //source id - 2 bytes
                msgBytesExtra[iOffset] = 0x10;
                iOffset++;
                msgBytesExtra[iOffset] = 0x00;
                iOffset++;
                //function code or MTI - 2 bytes
                msgBytesExtra[iOffset] = 0x09;
                iOffset++;
                msgBytesExtra[iOffset] = 0x97;
                iOffset++;
                //data length to follow
                msgBytesExtra[iOffset] = (byte)((iCSVLen >> 8) & 0xFF);
                iOffset++;
                msgBytesExtra[iOffset] = (byte)(iCSVLen & 0xFF);
                iOffset++;
                Array.Copy(msgBytes, 0, msgBytesExtra, iOffset, msgBytes.Length);
                iOffset += msgBytes.Length;
                msgBytesExtra[iOffset] = 0xFF;
                iOffset++;
                string hexString = BytesToHex(msgBytesExtra);
                Console.WriteLine(hexString);
                return hexString;
            }
            else
            {
                return "";
            }
        }
        static string BytesToHex(byte[] bytes)
        {
            StringBuilder hexBuilder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hexBuilder.AppendFormat("{0:x2}", b);
            return hexBuilder.ToString().ToUpper();
        }

    }
}
