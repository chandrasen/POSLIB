using Newtonsoft.Json;
using PosLibs.ECRLibrary.Common;
using PosLibs.ECRLibrary.Common.Interface;
using PosLibs.ECRLibrary.Model;
using Serilog;
using System.CodeDom.Compiler;

namespace PosLibs.ECRLibrary.Service
{
    public class TransactionService
    {
        private  ITransactionListener? _transactionListener;
        public IConnectionListener _connlistener=new ConnectionListener();
        private ConfigData _configData;
        private readonly ConnectionService _connectionService;
        public string _transactionRequest;
        public  string _transactionRequestBody;



        public TransactionService()
        {
            _connectionService = new ConnectionService();
            _transactionRequest = string.Empty;
            _transactionRequestBody = string.Empty;
            _configData = new ConfigData();
           
        }

        public void DoTransaction(string inputReqBody, int transactionType, ITransactionListener transactionListener)
        {
            try
            {
                Log.Debug("Inside DoTransaction Method");

                _transactionListener = transactionListener;
                _configData = _connectionService.GetConfigData() ?? new ConfigData();
                _configData.isAppidle = false;
                _connectionService.setConfiguration(_configData);
                PrepareTransaction(inputReqBody, transactionType);

                if (_configData?.isConnectivityFallBackAllowed == true)
                {
                    _configData.isAppidle = false;
                    _connectionService.setConfiguration(_configData);
                    ExecuteWithFallbackConnection();
                    _configData.isAppidle = true;
                    _connectionService.setConfiguration(_configData);
                }
                else
                {
                    _configData.isAppidle = false;
                    _connectionService.setConfiguration(_configData);
                    ExecuteSingleConnectionMode();
                }
            }
            catch (Exception ex)
            {
                HandleTransactionFailure("An error occurred during transaction: " + ex, PosLibConstant.GENERAL_EXCEPTION);
            }
            finally
            {
                Log.Information("======================================================================================================================================================");
            }
        }

        private void PrepareTransaction(string inputReqBody, int transactionType)
        {
            Log.Debug("entering execute preparetransaction()");
            _transactionRequestBody = TransactionRequestBody(inputReqBody, transactionType);
                 _transactionRequest = TransactionRequest(_transactionRequestBody);
                 Log.Information("transaction request body : " + _transactionRequestBody);
        }

        private void ExecuteWithFallbackConnection()
        {
            bool transactionSuccessful=false;
            Log.Debug("entering execute executewithfallbackconnection method");
            foreach (var communicationType in _configData.communicationPriorityList)
            {
                Log.Information("Communication Priority list:" +communicationType);
                if (communicationType == PosLibConstant.TCPIP || communicationType == PosLibConstant.COM)
                {
                     transactionSuccessful = TryExecuteCommunicationType(communicationType);
                    if (transactionSuccessful)
                    {
                        _configData.isAppidle = true;
                        _connectionService.setConfiguration(_configData);
                        break;
                    }
                }
            }
            if (!transactionSuccessful && _transactionListener!=null)
            {
                _transactionListener.OnFailure(PosLibConstant.AUTOFALLBACK_TXN_FAIELD_MSG,PosLibConstant.AUTOFALLBACK_TXN_FAIELD);
            }
        }
        private void ExecuteSingleConnectionMode()
        {
            Log.Debug("Entering Execute SingleConnectionMode");
            if (_configData?.connectionMode == PosLibConstant.TCPIP)
            {
                Log.Information("Excuted normal tcp/ip Transaction");
                ExecuteTcpIpTransaction();
            }
            else if (_configData?.connectionMode == PosLibConstant.COM)
            {
                Log.Information("Executed normal com transaction");
                ExecuteComTransaction();
            }
        }
        private bool TryExecuteCommunicationType(string communicationType)
        {
            Log.Debug("Entering Execute TryExecuteCommunicationType");
            if (communicationType == PosLibConstant.TCPIP)
            {
                Log.Information("Executed tcp/ip autofallback transaction method");
                return ExecuteTcpIpTransactionWithFallback();
            }
            else if (communicationType == PosLibConstant.COM)
            {
                Log.Information("Executed com autofallback Transaction method");
                return ExecuteAutoFallbackComTransaction();
            }
            return false;
        }

        private bool ExecuteTcpIpTransactionWithFallback()
        {
            try
            {
                if (_connectionService.ProcessOnlineTransaction(_configData.tcpIp, _configData.tcpPort))
                {
                    string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);
                    _connectionService.SendTcpIpTxnData(encryptedRequest);
                    Log.Information("send encrypted tcp/ip auto fallback txn request:" + encryptedRequest);
                    string responseString = _connectionService.ReceiveTcpIpTxnData();
                    HandleTransactionResponse(responseString);
                    return true;

                }
                else
                {
                    Log.Information("autofallback tcp/ip transaction failed");
                }

            }
            catch (Exception ex)
            {
                Log.Error("TCP/IP transaction failed: " + ex);
            }

            return false;
        }
        private bool ExecuteTcpIpTransaction()
        {
            try
            {
                if (_connectionService.ProcessOnlineTransaction(_configData.tcpIp, _configData.tcpPort))
                {   
                    string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);
                    _connectionService.SendTcpIpTxnData(encryptedRequest);
                    Log.Information("send encrypted TCP IP txn request:" + encryptedRequest);
                    string responseString = _connectionService.ReceiveTcpIpTxnData();
                    HandleTransactionResponse(responseString);
                    _configData.isAppidle = true;
                    _connectionService.setConfiguration(_configData);
                    return true;
                }
                else
                {
                    _configData.isAppidle = true;
                    _transactionListener?.OnFailure(PosLibConstant.TCPIPFAIELD_TXN_ERROR, PosLibConstant.CON_FAILD_EXCEPTION);
                }
            }
            catch (Exception ex)
            {
                _configData.isAppidle = true;
                _connectionService.setConfiguration(_configData);
                Log.Error("TCP/IP transaction failed: " + ex);
            }

            return false;
        }
        private bool ExecuteAutoFallbackComTransaction()
        {

             if (_connectionService.ComTransactionProcess(_configData.commPortNumber))
                {
                    string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);
                    _connectionService.SendCOMTxnReq(encryptedRequest);
                    Log.Information("send encrypted com txn request:" + encryptedRequest);
                    string responseString = _connectionService.ReceiveCOMTxnrep();
                    HandleTransactionResponse(responseString);
                    return true;
               }
            else
            {
                Log.Information("autofallback com transaction failed");
            }
            return false;
        }

        private bool ExecuteComTransaction()
        {
                if (_connectionService.IsComDeviceConnected(_configData.commPortNumber, _connlistener))
                {
                    string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);
                    _connectionService.SendCOMTxnReq(encryptedRequest);
                    Log.Information("send encrypted com txn request:" + encryptedRequest);
                    string responseString = _connectionService.ReceiveCOMTxnrep();
                    HandleTransactionResponse(responseString);
                    _configData.isAppidle = true;
                    _connectionService.setConfiguration(_configData);
                    return true;
                }
                else
                {
                    _transactionListener?.OnFailure(PosLibConstant.COMFAIELD_TXN_ERROR, PosLibConstant.CON_FAILD_EXCEPTION);
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
            Log.Error("transaction failed: "+errorMessage);
            _transactionListener?.OnFailure(errorMessage, int.Parse(errorCode));
        }
        private  string TransactionRequestBody(string requestbody, int txntype)
        {
            return CommaUtil.stringToCsv(txntype, requestbody);
        }
        public string TransactionRequest(string requestbody)
        {
            Log.Debug("Enter Transaction Request Method");
            TransactionRequest trnrequest = new TransactionRequest();
            trnrequest.cashierId = "";
            trnrequest.isDemoMode = false;
            trnrequest.msgType = 6;
            trnrequest.pType = 1;
            trnrequest.requestBody = requestbody;
            string jsontransrequest = JsonConvert.SerializeObject(trnrequest);
            string newrquestBody = CommaUtil.HexToCsv(requestbody);
            _transactionRequestBody = newrquestBody;
            _transactionRequest = CommaUtil.ReplaceRequestBody(jsontransrequest, newrquestBody);
            Log.Information("whole transaction api" + jsontransrequest);
            return jsontransrequest;
        }

    }




}
