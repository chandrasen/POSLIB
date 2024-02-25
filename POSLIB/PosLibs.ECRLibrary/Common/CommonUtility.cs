using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public static class CommonUtility
    {
        /// <summary>
        /// string hexString = "020230393030313230030DB1";
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] HexStringToByteArray(string hexString)
        {
            return Common.CommaUtil.HexToBytes(hexString);
            //int length = hexString.Length;
            //byte[] byteArray = new byte[length / 2];
            //for (int i = 0; i < length; i += 2)
            //{
            //    byteArray[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            //}
            //return byteArray;
        }

        public static string GetByteSliceToHexaString(byte[] inputbyteArray, int fromByte, int toByte)
        {
            byte[] extractedBytes = new byte[toByte - fromByte + 1];
            Array.Copy(inputbyteArray, fromByte - 1, extractedBytes, 0, toByte - fromByte +1);

            // Convert extracted bytes back to hexadecimal string
            string result = BitConverter.ToString(extractedBytes).Replace("-", "");
            return result;
        }

        public static string ByteArrayToHexaString(byte[] input)
        {
            return CommaUtil.BytesToHex(input);
        }
        public static byte[] ConvertAsciiToByteArray(string asciiString)
        {
            return Encoding.ASCII.GetBytes(asciiString);
        }
     }
}
