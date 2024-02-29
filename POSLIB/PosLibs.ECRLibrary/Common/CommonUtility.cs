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

        public static string ConvertAsciiToHexaString(string asciiString)
        {
            return CommaUtil.BytesToHex(Encoding.ASCII.GetBytes(asciiString));
        }

        public static string GetTransactioType(string transTypeSelectedPos)
        {
            string transactionType = string.Empty;
            if (transTypeSelectedPos != null)
            {
                if (transTypeSelectedPos.ToString() == TxnConstant.SALE)
                {
                    transactionType = "4001";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.UPI_SALE_REQUEST)
                {
                    transactionType = "5120";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.BHARAT_QR_SALE_RQUEST)
                {
                    transactionType = "5123";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.REFUND)
                {
                    transactionType = "4002";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.SALE_COMPLETE)
                {
                    transactionType = "4008";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.VOID)
                {
                    transactionType = "4006";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.TIP_ADJUST)
                {
                    transactionType = "4015";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.ADJUST)
                {
                    transactionType = "4005";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.SETTLEMENT)
                {
                    transactionType = "6001";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.BRAND_EMI)
                {
                    transactionType = "5002";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.BANK_EMI)
                {
                    transactionType = "5101";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.CASH_ONLY)
                {
                    transactionType = "4503";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.PAYBACK_VOID)
                {
                    transactionType = "4403";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.PAYBACK_EARN)
                {
                    transactionType = "4404";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.SALE_WITH_CASH)
                {
                    transactionType = "4502";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.PRE_AUTH_TXN)
                {
                    transactionType = "4007";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.WALLETPAY)
                {
                    transactionType = "5102";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.WALLETLAOD)
                {
                    transactionType = "5103";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.WALLETVOID)
                {
                    transactionType = "5104";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.TWID_SALE)
                {
                    transactionType = "5131";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.TWID_GET)
                {
                    transactionType = "5122";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.TWID_VOID)
                {
                    transactionType = "5121";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.AmazonPayBarcode)
                {
                    transactionType = "5129";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.EMI_as_Single_Txn_Type_for_Brand_and_Bank_EMI)
                {
                    transactionType = "5505";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.SALE_CARD_BRAND_EMI)
                {
                    transactionType = "5003";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.QC_Redemption)
                {
                    transactionType = "4205";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.Gift_Code)
                {
                    transactionType = "4113";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.Void_Cardless_Bank)
                {
                    transactionType = "5031";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.Sale_With_Without_Instant_Discount)
                {
                    transactionType = "4603";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.Magic_Pin_Sale)
                {
                    transactionType = "4109";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.Magic_PIN_Void)
                {
                    transactionType = "4110";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.Zest_MoNAy_Invoice_Sale)
                {
                    transactionType = "5367";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.Zest_MoNAy_Product_Sale)
                {
                    transactionType = "5370";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.Zest_MoNAy_Void)
                {
                    transactionType = "5369";
                }
                else if (transTypeSelectedPos.ToString() == TxnConstant.HDFC_Flexipay)
                {
                    transactionType = "5030";
                }

            }
            return transactionType;
        }
     }
}
