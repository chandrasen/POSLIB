using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PosLibs.ECRLibrary.Model;
using Serilog;

namespace PosLibs.ECRLibrary.Common
{
    public class CommaUtil : ICommaUtil
    {
        protected CommaUtil() { }

        //IP Address Validaiton
        public  bool CheckIPAddress(string IP)
        {
            try
            {
                if (!IPAddress.TryParse(IP, out IPAddress? parsedIPAddress) || parsedIPAddress == null)
                    return false;
                string[] octets = IP.Split('.');
                if (octets.Length != 4)
                    return false;
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid IP Exception");
            }
            return true;
        }

        //Convert string to Csv format
        public static string stringToCsv(int txntype,string amount)
        {
            Log.Debug("Inside StringToCsv method");
            
            string value = "TX12345678";
            string fullrequbody = txntype.ToString() + "," +
                value + "," + amount + "," + "," + "," + "," + "," + "," + ",";
            string HaxDecimalreqbody=  GetPaymentPacket(fullrequbody);
            Log.Information("Txn Request Body in Csv format:" + HaxDecimalreqbody);
            return HaxDecimalreqbody;
        }

        //getPaymentPacket method
        static string GetPaymentPacket(string csvData)
        {
            Log.Debug("Inside GetPaymentPacket method");
            if (!string.IsNullOrEmpty(csvData))
            {
                int iOffset = 0;
                byte[] msgBytes = Encoding.UTF8.GetBytes(csvData);
                int iCSVLen = msgBytes.Length;
                int finalMsgLen = iCSVLen + 7;
                byte[] msgBytesExtra = new byte[finalMsgLen];
            
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
                string hexString = BytesToHex(msgBytesExtra);
                Console.WriteLine(hexString);
                Log.Information("Txn Request Body HexDecimal String" + hexString);
                return hexString;
            }
            else
            {
                Log.Information("Txn Request Body Hexdecimal String :" + " ");
                return "";
            }
        }

        //Convert Byte To Hex String
        static string BytesToHex(byte[] bytes)
        {
            StringBuilder hexBuilder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hexBuilder.AppendFormat("{0:x2}", b);
            return hexBuilder.ToString().ToUpper();
        }

    }
}
