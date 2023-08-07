using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    /// <summary>
    /// This class used to encrypt and decrypt the transaction request and response
    /// </summary>
    public static class XorEncryption
    {
        public static string EncryptDecrypt(string input)
        {
            char[] key = "F293A091D0104091BFD51F24CD02E4C6".ToCharArray(); 
            StringBuilder output = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                output.Append((char)(input[i] ^ key[i % key.Length]));
            }

            return output.ToString();
        }
    }
}
