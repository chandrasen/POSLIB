using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PosLibs.ECRLibrary.Common;
using PosLibs.ECRLibrary.Model;
using Serilog;

namespace PosLibs.ECRLibrary.Service
{
    public class TransacationService
    {
        public TransacationService() { }
        private ITransactionListener? trasnlistener;
        ConfigData configdata = new ConfigData();
        readonly ConnectionService conobj = new ConnectionService();
        public string transactionRequest(string requestbody)
        {
            TransactionRequest trnrequest = new TransactionRequest();
            trnrequest.cashierId = "";
            trnrequest.pType = "1";
            trnrequest.msgType = "0";
            trnrequest.requestBody = requestbody;
            trnrequest.isDemoMode = false;
            string jsontransrequest = JsonConvert.SerializeObject(trnrequest);

            return jsontransrequest;

        }
        public string transrequestBody(string requestbody, int txntype)
        {
            return CommaUtil.stringToCsv(txntype, requestbody);
        }
        public void doTransaction(string inputReqBody, int transactionType, ITransactionListener transactionListener)
        {
            Log.Debug("Inside doTransaction Method");
            this.trasnlistener = transactionListener;
            string responseString = string.Empty;
            byte[] buffer = new byte[50000];
            configdata = conobj.getConfigData();
            string req = transrequestBody(inputReqBody, transactionType);
            string transactionRequestbody = transactionRequest(req);
            string encrypttxnrequst = XorEncryption.EncryptDecrypt(transactionRequestbody);

            Log.Information("Txn request:-" + transactionRequestbody);

            try
            {
                Log.Information("isConnectionFollwedBack:" + configdata?.isConnectivityFallBackAllowed);

                if (configdata?.isConnectivityFallBackAllowed == true)
                {
                    bool transactionSuccessful = false;

                    for (int i = 0; i < configdata?.communicationPriorityList?.Length; i++)
                    {
                        Log.Information("communication Priority List " + ":-" + i + " " + configdata?.communicationPriorityList[i]);
                        if (configdata?.communicationPriorityList[i] == PinLabsEcrConstant.TCPIP || configdata?.communicationPriorityList[i] == PinLabsEcrConstant.COM)
                        {
                            try
                            {
                                if (configdata?.communicationPriorityList[i] == PinLabsEcrConstant.TCPIP)
                                {
                                    try
                                    {
                                        conobj.sendTcpIpTxnData(encrypttxnrequst);
                                        responseString = conobj.receiveTcpIpTxnData();
                                        transactionSuccessful = true;
                                    }
                                    catch (SocketException e)
                                    {
                                        transactionSuccessful = false;
                                        Log.Information("Txn failed Due to TCP/IP Connection "+e);
                                    }
                                    catch (ObjectDisposedException ex)
                                    {
                                        Console.Write("Please check TCP/IP Connection"+ex);
                                        transactionListener?.onFailure("Please check TCP/IP connection", PinLabsEcrConstant.CON_FAILD_EXCEPTION);
                                    }

                                    Console.WriteLine("Transaction Successful:" + transactionSuccessful);
                                }
                                else if (configdata?.communicationPriorityList[i] == PinLabsEcrConstant.COM)
                                {
                                    Console.WriteLine("Selected Priorit:-" + configdata?.communicationPriorityList[i]);
                                    try
                                    {
                                        conobj.sendData(encrypttxnrequst);
                                        Array.Clear(buffer, 0, buffer.Length);
                                        responseString = conobj.receiveCOMTxnrep();
                                        Console.WriteLine("COM Transaction Response:" + responseString);
                                        transactionSuccessful = true;
                                    }
                                    catch (SocketException)
                                    {
                                        transactionSuccessful = false;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        transactionSuccessful = false;
                                    }
                                    catch (IOException e)
                                    {
                                        Log.Error(PinLabsEcrConstant.IO_EXC_MSG, e);
                                        transactionSuccessful = false;
                                    }
                                }
                            }
                            catch (SocketException)
                            {
                                transactionSuccessful = false;
                            }

                            if (transactionSuccessful)
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (configdata?.connectionMode == PinLabsEcrConstant.TCPIP)
                    {
                        bool isSend = false;
                        try
                        {
                            if (conobj.isOnlineConnection(configdata.tcpIp, configdata.tcpPort))
                            {
                                isSend = conobj?.sendTcpIpTxnData(encrypttxnrequst) == true;
                                if (isSend)
                                {
                                    Console.WriteLine("Selected ConnectionMode" + configdata?.connectionMode);
                                    Log.Information("Selected ConnectionMode:-" + configdata?.connectionMode);
                                    responseString = conobj.receiveTcpIpTxnData();
                                    Log.Information("TCP/IP connection Txn Response:" + responseString);
                                }
                                else
                                {
                                    Console.Write("Please check TCP/IP Connection");
                                    Log.Warning("Please check TCP/IP Connection");
                                    transactionListener?.onFailure("Please check TCP/IP connection", PinLabsEcrConstant.TXN_FAILD);
                                }
                            }
                        }
                        catch (ObjectDisposedException ex)
                        {
                            Console.Write("Please check TCP/IP Connection"+ex);
                            Log.Error("Please check TCP/IP connection" + PinLabsEcrConstant.TXN_FAILD);
                            transactionListener?.onFailure("Please check TCP/IP connection", PinLabsEcrConstant.TXN_FAILD);
                        }
                    }
                    else if (configdata?.connectionMode == PinLabsEcrConstant.COM)
                    {
                        if (isNetworkAvailabe())
                        {
                            Log.Information("Selected ConnectionMode:-" + configdata?.connectionMode);
                            Console.WriteLine("Selected ConnectionMode" + configdata?.connectionMode);
                            conobj.sendCOMTXNData(encrypttxnrequst);
                            Array.Clear(buffer, 0, buffer.Length);
                            responseString = conobj.receiveCOMTxnrep();
                            Console.WriteLine("COM Transaction Response:" + responseString);
                        }
                        else
                        {
                            transactionListener?.onFailure("Please Connected with Serial Cable", PinLabsEcrConstant.TXN_FAILD);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Transaction Not Found");
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }
            catch (TimeoutException)
            {
                Log.Error("Transaction Timeout");
                transactionListener?.onFailure("Transaction TimeOut", PinLabsEcrConstant.TIME_OUT_EXCEPTION);
            }
            catch (SocketException e)
            {
                Log.Error("Due to Socket Exception Transaction Failed"+e);
                trasnlistener?.onFailure("Transaction Failed", PinLabsEcrConstant.SOCK_EXCEPTION);
            }

            if (trasnlistener != null)
            {
                string decreresponse = XorEncryption.EncryptDecrypt(responseString);

                transactionListener?.onSuccess(decreresponse);
            }
            else
            {
                if (transactionListener != null)
                {
                    trasnlistener?.onFailure("Transaction Failed", PinLabsEcrConstant.TXN_FAILD);
                }
            }
        }

        public bool isNetworkAvailabe()
        {
            bool isavilabe = false;

            configdata = conobj.getConfigData();
            if (configdata != null)
            {
                if (conobj.isComDeviceConnected(configdata.commPortNumber))
                {
                    isavilabe = true;
                }
                else
                {
                    isavilabe = false;
                }

            }
            return isavilabe;
        }
    }
}
