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
    public  class TransacationService
    {
        public TransacationService() { }
       

     
        public ITransactionListener trasnlistener;
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
        public string transrequestBody(string requestbody)
        {
            return requestbody;
        }
 public void doTransaction(string inputReqBody, int transactionType, ITransactionListener transactionListener)
  {
    Log.Debug("Inside doTransaction Method");
    this.trasnlistener = transactionListener;
    string responseString = " ";
    bool res = false;
    byte[] buffer = new byte[50000];
    configdata = conobj.getConfigData();
    TransactionReponse transactionReponse = new TransactionReponse();

    string req = transrequestBody(inputReqBody);

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
                if (configdata?.communicationPriorityList[i] == "TCP/IP" || configdata?.communicationPriorityList[i] == "COM")
                {
                    try
                    {
                        if (configdata?.communicationPriorityList[i] == "TCP/IP")
                        {
                            try
                            {
                                conobj?.sendTcpIpTxnData(encrypttxnrequst);
                                responseString = conobj?.receiveTcpIpTxnData();
                                transactionSuccessful = true;
                            }
                            catch (SocketException e)
                            {
                                transactionSuccessful = false;
                                Log.Information("Txn failed Due to TCP/IP Connection ");
                            }
                            catch (ObjectDisposedException ex)
                            {
                                Console.Write("Please check TCP/IP Connection");
                                transactionListener?.onFailure("Please check TCP/IP connection", 1005);
                            }

                            Console.WriteLine("Transaction Successful:" + transactionSuccessful);
                        }
                        else if (configdata?.communicationPriorityList[i] == "COM")
                        {
                            Console.WriteLine("Selected Priorit:-" + configdata?.communicationPriorityList[i]);
                            try
                            {
                                conobj?.sendData(encrypttxnrequst);
                                Array.Clear(buffer, 0, buffer.Length);
                                responseString = conobj?.receiveCOMTxnrep();
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
            if (configdata?.connectionMode == "TCP/IP")
            {
                bool isSend = false;
                try
                {
                    if (conobj?.isOnlineConnection(configdata.tcpIp, configdata.tcpPort) == true)
                    {
                        isSend = conobj?.sendTcpIpTxnData(encrypttxnrequst) == true;
                        if (isSend)
                        {
                            Console.WriteLine("Selected ConnectionMode" + configdata?.connectionMode);
                            Log.Information("Selected ConnectionMode:-" + configdata?.connectionMode);
                            responseString = conobj?.receiveTcpIpTxnData();
                            Log.Information("TCP/IP connection Txn Response:" + responseString);
                        }
                        else
                        {
                            Console.Write("Please check TCP/IP Connection");
                            Log.Warning("Please check TCP/IP Connection");
                            transactionListener?.onFailure("Please check TCP/IP connection", 1005);
                        }
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    Console.Write("Please check TCP/IP Connection");
                    Log.Error("Please check TCP/IP connection" + 1005);
                    transactionListener?.onFailure("Please check TCP/IP connection", 1005);
                }
            }
            else if (configdata?.connectionMode == "COM")
            {
                if (isNetworkAvailabe())
                {
                    Log.Information("Selected ConnectionMode:-" + configdata?.connectionMode);
                    Console.WriteLine("Selected ConnectionMode" + configdata?.connectionMode);
                    conobj?.sendCOMTXNData(encrypttxnrequst);
                    Array.Clear(buffer, 0, buffer.Length);
                    responseString = conobj?.receiveCOMTxnrep();
                    Console.WriteLine("COM Transaction Response:" + responseString);
                }
                else
                {
                    transactionListener?.onFailure("Please Connected with Serial Cable", 1005);
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
        transactionListener?.onFailure("Transaction TimeOut", 1003);
    }
    catch (SocketException e)
    {
        Log.Error("Due to Socket Exception Transaction Failed");
        trasnlistener?.onFailure("Transaction Failed", 1001);
    }

    if (trasnlistener != null)
    {
        transactionListener?.onSuccess(responseString);
    }
    else
    {
        if (transactionListener != null)
        {
            trasnlistener?.onFailure("Transaction Failed", 1001);
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
