using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PosLibs.ECRLibrary.Model;
using Serilog;

namespace PosLibs.ECRLibrary.Common
{
    public static class CommaUtil 
    {

        //IP Address Validaiton

        /// <summary>
        /// this method replace the requesbody with csv data
        /// </summary>
        /// <param name="inputJson"></param>
        /// <param name="newRequestBody"></param>
        /// <returns></returns>
        public static string ReplaceRequestBody(string inputJson, string newRequestBody)
        {
            try
            {
                dynamic parsedJson = JToken.Parse(inputJson);
                parsedJson["requestBody"] = newRequestBody;

                string modifiedJson = parsedJson.ToString();
                return modifiedJson;
            }
            catch
            {
                return "\"received failed: 2204\"";
            }
        }
        //Convert string to Csv format
        public static string stringToCsv(int txntype,string amount)
        {
            Log.Debug("Inside StringToCsv method");
            if (txntype == int.Parse("6001"))
            {
                string fullrequbody = txntype.ToString() + "," +
                     "," + "," + "," + "," + "," + "," + "," + ",";
                string HaxDecimalreqbody = GetPaymentPacket(fullrequbody);
                return HaxDecimalreqbody;

            }
            else {

                string value = "TX12345678";
                string fullrequbody = txntype.ToString() + "," +
                    value + "," + amount + "," + "," + "," + "," + "," + "," + ",";
                string HaxDecimalreqbody = GetPaymentPacket(fullrequbody);
                return HaxDecimalreqbody;
            }
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

       public static string HexToString(string hexValue)
        {
            byte[] bytes = Regex.Matches(hexValue, ".{2}")
                                 .Cast<Match>()
                                 .Select(m => Convert.ToByte(m.Value, 16))
                                 .ToArray();
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        /// <summary>
        /// this method convert the hexstring to normal csv data or string 
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public  static string HexToCsv(string hexString)
        {
            byte[] hexBytes = HexToBytes(hexString);

            if (hexBytes != null && hexBytes.Length >= 7)
            {
                int csvLength = (hexBytes[4] << 8) | hexBytes[5];
                string csvData = Encoding.UTF8.GetString(hexBytes, 6, csvLength);
                return csvData;
            }
            else
            {
                return "";
            }
        }

        //this method Extract the Hex decimal value
        public static string ExtractHexValue(string inputJson)
        {
            try
            {
                Log.Information("Received Response body:" + inputJson);
                dynamic parsedJson = JsonConvert.DeserializeObject(inputJson);
                string responseBody = parsedJson.responseBody ?? string.Empty;
                string hexValue = Regex.Replace(responseBody, "[^0-9a-fA-F]", "");
                string hexresult =   HexToString(hexValue);
                return hexresult;
            }
            catch
            {
                return "\"received failed: 2204\""; 
            }
        }
        /// <summary>
        /// this mehtod use inside HexToCsv data to remove spaces and convert it to bytes array
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        static byte[] HexToBytes(string hex)
        {
            hex = hex.Replace(" ", ""); // Remove spaces if any
            int length = hex.Length;

            if (length % 2 != 0)
            {
                hex = "0" + hex; // Add a leading zero if the length is odd
                length++;
            }

            byte[] bytes = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        public static string ConvertHexdecimalToTransactionResponse(string transactionResponse)
        {
            string decreresponse = XorEncryption.EncryptDecrypt(transactionResponse);
            string decreptresponse = CommaUtil.ExtractHexValue(decreresponse);
            return decreptresponse;
        }

    }
}
