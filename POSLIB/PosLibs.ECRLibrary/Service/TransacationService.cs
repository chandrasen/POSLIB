using Newtonsoft.Json;
using PosLibs.ECRLibrary.Common;
using PosLibs.ECRLibrary.Common.Interface;
using PosLibs.ECRLibrary.Model;
using Serilog;
using System;
using System.CodeDom.Compiler;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PosLibs.ECRLibrary.Service
{
    public class TransactionService
    {

        private  ITransactionListener? _transactionListener;
        private ConfigData _configData = new ConfigData();
        private readonly ConnectionService _connectionService;
        private string _transactionRequest;
        private  string _transactionRequestBody;
        public TransactionService()
        {
            Log.Information("Transcation Service");
            _connectionService = new ConnectionService();
            _transactionRequest = string.Empty;
            _transactionRequestBody = string.Empty;
            _configData = new ConfigData();
           
        }

        public void doTransaction(string inputReqBody, int transactionType, ITransactionListener transactionListener)
        {
            try
            {
                Log.Debug("Inside DoTransaction Method");
                Log.Information("Entering doTrnsaction method");
                _transactionListener = transactionListener;
                _connectionService.getConfiguration(out _configData);
                _configData.isAppidle = false;
                _connectionService.setConfiguration(_configData);
                PrepareTransaction(inputReqBody, transactionType);
                string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);

                if (_configData?.isConnectivityFallBackAllowed == true)
                {
                    Log.Information("Auto fallback transaction process :" + _configData.isConnectivityFallBackAllowed);
                    _configData.isAppidle = false;
                    _connectionService.setConfiguration(_configData);
                    Log.Information("Autofallback plain txn request:" + _transactionRequest);
                    Log.Information("encrypted  request:" + encryptedRequest);
                    ExecuteWithFallbackConnection();
                }
                else
                {
                    Log.Information("plain txn request:" + _transactionRequest);
                    Log.Information("encrypted  request:" + encryptedRequest);
                    ExecuteSingleConnectionMode();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                HandleTransactionFailure("An error occurred during transaction: ", PosLibConstant.GENERAL_EXCEPTION);
                _configData.isAppidle = true;
                _connectionService.setConfiguration(_configData);
            }
            finally
            {
                Log.Information("Exiting Transaction Service");
            }
        }

        private void PrepareTransaction(string inputReqBody, int transactionType)
        {
            Log.Debug("entering execute preparetransaction()");
            _transactionRequestBody = TransactionRequestBody(inputReqBody, transactionType);
            _transactionRequest = TransactionRequest(_transactionRequestBody);
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
                Log.Information("Transaction failed");
                _transactionListener.OnFailure(PosLibConstant.AUTOFALLBACK_TXN_FAIELD_MSG,PosLibConstant.AUTOFALLBACK_TXN_FAIELD);
                _configData.isAppidle = true;
                _connectionService.setConfiguration(_configData);
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
                if (_connectionService.ProcessOnlineConnection(_configData.tcpIp, _configData.tcpPort))
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
                Console.WriteLine(ex.StackTrace);
                return false;
            }

            return false;
        }
        private bool ExecuteTcpIpTransaction()
        {
            try
            {
                if (_connectionService.ProcessOnlineConnection(_configData.tcpIp, _configData.tcpPort))
                {
                    string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);
                    _connectionService.SendTcpIpTxnData(encryptedRequest);
                    string responseString = _connectionService.ReceiveTcpIpTxnData();
                    HandleTransactionResponse(responseString);
                    _configData.isAppidle = true;
                    _connectionService.setConfiguration(_configData);
                    return true;
                }
                else
                {
                   
                    _transactionListener?.OnFailure(PosLibConstant.TCPIPFAIELD_TXN_ERROR, PosLibConstant.TXN_FAILD_EXCEPTION);
                    _configData.isAppidle = true;
                    _connectionService.setConfiguration(_configData);
                }
            }
            catch (Exception ex)
            {
                _configData.isAppidle = true;
                _transactionListener?.OnFailure(PosLibConstant.TCPIPFAIELD_TXN_ERROR, PosLibConstant.TXN_FAILD_EXCEPTION);
                _connectionService.setConfiguration(_configData);
                Console.WriteLine(ex.ToString());
                Log.Error("TCP/IP transaction failed: ");
            }
            return false;
        }
        private bool ExecuteAutoFallbackComTransaction()
        {
             if (_connectionService.ComTransactionProcess(_configData.commPortNumber))
                {
                    string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);
                    _connectionService.SendCOMTxnReq(encryptedRequest);
                    string responseString = _connectionService.ReceiveCOMTxnrep();
                    HandleTransactionResponse(responseString);
                    return true;
               }
            return false;
        }

        private bool ExecuteComTransaction()
        {
                if (_connectionService.ComTransactionProcess(_configData.commPortNumber))
                {
                     string encryptedRequest = XorEncryption.EncryptDecrypt(_transactionRequest);
                    _connectionService.SendCOMTxnReq(encryptedRequest);
                    string responseString = _connectionService.ReceiveCOMTxnrep();
                    HandleTransactionResponse(responseString);
                    _configData.isAppidle = true;
                    _connectionService.setConfiguration(_configData);
                    return true;
                }
                else
                {
                _transactionListener?.OnFailure(PosLibConstant.COMFAIELD_TXN_ERROR, PosLibConstant.COM_FAILD_EXCEPTION);
                _configData.isAppidle = true;
                _connectionService.setConfiguration(_configData);
            }
            return false;
        }
        private void HandleTransactionResponse(string responseString)
        {
            Log.Information("Received Encrypted Transaction response: " + responseString);
            string decryptedResponse = CommaUtil.ConvertHexdecimalToTransactionResponse(responseString);
            Log.Information("Decrypted transaction response: " + decryptedResponse);
            _transactionListener?.OnSuccess(decryptedResponse);
        }
        private void HandleTransactionFailure(string errorMessage, string errorCode)
        {
            Log.Error(ExceptionConst.TRANSACTIONFAIL+errorMessage);
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
            return jsontransrequest;
        }
      
    }

}
