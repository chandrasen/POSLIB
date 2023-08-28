using Newtonsoft.Json;
using PosLibs.ECRLibrary.Common;
using PosLibs.ECRLibrary.Common.Interface;
using PosLibs.ECRLibrary.Model;
using Serilog;
namespace PosLibs.ECRLibrary.Service
{
    public class TransactionService
    {
        private  ITransactionListener? _transactionListener;
        private ConfigData _configData;
        private readonly ConnectionService _connectionService;
        private string _transactionRequest;
        public string _transactionRequestBody = string.Empty;
        

        public TransactionService()
        {
            _connectionService = new ConnectionService();
            _transactionRequest = string.Empty;
            _configData = new ConfigData();
        }

        public void DoTransaction(string inputReqBody, int transactionType, ITransactionListener transactionListener)
        {
            try
            {
                Log.Debug("Inside DoTransaction Method");

                _transactionListener = transactionListener;
                _configData = _connectionService.GetConfigData() ?? new ConfigData();

                PrepareTransaction(inputReqBody, transactionType);

                if (_configData?.isConnectivityFallBackAllowed == true)
                {
                    ExecuteWithFallbackConnection();
                }
                else
                {
                    ExecuteSingleConnectionMode();
                }
            }
            catch (Exception ex)
            {
                HandleTransactionFailure("An error occurred during transaction: " + ex, PinLabsEcrConstant.GENERAL_EXCEPTION);
            }
            finally
            {
                Log.Information("======================================================================================================================================================");
            }
        }

        private void PrepareTransaction(string inputReqBody, int transactionType)
        {
            string _transactionRequestBody = TransactionRequestBody(inputReqBody, transactionType);
                 _transactionRequest = TransactionRequest(_transactionRequestBody);
                 Log.Information("Transaction request: " + _transactionRequestBody);
        }

        private void ExecuteWithFallbackConnection()
        {
            foreach (var communicationType in _configData.communicationPriorityList)
            {
                if (communicationType == PinLabsEcrConstant.TCPIP || communicationType == PinLabsEcrConstant.COM)
                {
                    bool transactionSuccessful = TryExecuteCommunicationType(communicationType);

                    if (transactionSuccessful)
                        break;
                }
            }
        }
        private void ExecuteSingleConnectionMode()
        {
            if (_configData?.connectionMode == PinLabsEcrConstant.TCPIP)
            {
                ExecuteTcpIpTransaction();
            }
            else if (_configData?.connectionMode == PinLabsEcrConstant.COM)
            {
                ExecuteComTransaction();
            }
        }

        private bool TryExecuteCommunicationType(string communicationType)
        {
            if (communicationType == PinLabsEcrConstant.TCPIP)
            {
                return ExecuteTcpIpTransaction();
            }
            else if (communicationType == PinLabsEcrConstant.COM)
            {
                return ExecuteComTransaction();
            }

            return false;
        }

        private bool ExecuteTcpIpTransaction()
        {
            try
            {
                if (_connectionService.IsOnlineConnection(_configData.tcpIp, _configData.tcpPort))
                {
                    string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);
                    _connectionService.SendTcpIpTxnData(encryptedRequest);

                    string responseString = _connectionService.ReceiveTcpIpTxnData();
                    HandleTransactionResponse(responseString);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("TCP/IP transaction failed: " + ex);
            }

            return false;
        }

        private bool ExecuteComTransaction()
        {
            try
            {
                if (_connectionService.IsComDeviceConnected(_configData.commPortNumber))
                {
                    string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);
                    _connectionService.SendData(encryptedRequest);

                    string responseString = _connectionService.ReceiveCOMTxnrep();
                    HandleTransactionResponse(responseString);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("COM transaction failed: " + ex);
            }

            return false;
        }

        private void HandleTransactionResponse(string responseString)
        {
            string decryptedResponse = CommaUtil.ConvertHexdecimalToTransactionResponse(responseString);
            Log.Information("Received transaction response: " + decryptedResponse);
            _transactionListener?.OnSuccess(decryptedResponse);
        }

        private void HandleTransactionFailure(string errorMessage, string errorCode)
        {
            Log.Error(errorMessage);
            _transactionListener?.OnFailure(errorMessage, int.Parse(errorCode));
        }

        private string TransactionRequestBody(string requestbody, int txntype)
        {
            return CommaUtil.stringToCsv(txntype, requestbody);
        }

        public string TransactionRequest(string requestbody)
        {
            TransactionRequest trnrequest = new TransactionRequest();
            trnrequest.cashierId = "";
            trnrequest.isDemoMode = false;
            trnrequest.msgType = 6;
            trnrequest.pType = 1;
            trnrequest.requestBody = requestbody;
            string jsontransrequest = JsonConvert.SerializeObject(trnrequest);
            string newrquestBody = CommaUtil.HexToCsv(requestbody);
            _transactionRequest = CommaUtil.ReplaceRequestBody(jsontransrequest, newrquestBody);
            _transactionRequestBody = _transactionRequest;
            return jsontransrequest;
        }

    }




}
