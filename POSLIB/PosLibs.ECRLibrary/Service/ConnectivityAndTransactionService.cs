using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PosLibs.ECRLibrary.Common;
using PosLibs.ECRLibrary.Model;
using PosLibs.ECRLibrary.LoggerFile;

namespace PosLibs.ECRLibrary.Service
{
    public class ConnectivityAndTransactionService
    {
        public ConnectivityAndTransactionService() { }

        public static Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static SerialPort serial = new SerialPort();

        public  string PortCom;
        public string IPortCom;
        public string bafudRateCom = string.Empty;
        public string parityCom = string.Empty;
        public int dataBitsCom;
        public int stopBitsCom;

        public Boolean responseboolean;

    



        public bool checkComConn()
        {
            bool status = serial.IsOpen;
            return status;

        }
        private const string PortSettingsFilePath = "C:\\Users\\prasanta.pande\\Desktop\\poscontroller\\POSController\\PosLibs.ECRLibrary\\LoggerFile\\Configure.json";

        private ConfigData getConfigData()
        {
            ConfigData configdata = new ConfigData();
            if (File.Exists(PortSettingsFilePath))
            {
                string json = File.ReadAllText(PortSettingsFilePath);
                configdata = JsonConvert.DeserializeObject<ConfigData>(json);
                return configdata;
            }

            // Default port number if the file doesn't exist
            return configdata;
        }

        private void SaveConfig(ConfigData configData)
        {
            ConfigData portSettings = new ConfigData { tcpIp = configData.tcpIp,
                tcpPort=configData.tcpPort,commPortNumber=configData.commPortNumber,connectionMode=configData.connectionMode };
            string json = JsonConvert.SerializeObject(portSettings);
            File.WriteAllText(PortSettingsFilePath, json);
        }

        public Boolean AutoConnect()
        {
            Boolean value = false; 
            ConfigData configdata = getConfigData();
            if(configdata != null)
            {
               
               
                 string connectionMode = configdata.connectionMode;
                if (connectionMode == "TCP/IP")
                {
                    string tcpip = configdata.tcpIp.ToString();
                    int tcpport = configdata.tcpPort;
                    value = isOnlineConnection(tcpip, tcpport);
                }
                else if(connectionMode=="COM")
                {
                    int commport = configdata.commPortNumber;
                    value = doCOMConnection(commport, ComConstants.BAUDRATECOM, ComConstants.PARITYCOM, ComConstants.DATABITSCOM, ComConstants.STOPBITSCOM);
                }
                else
                {
                    //MessageBox.Show("Please connect with Termianl");
                }
             
                 
            }

            return value;

      }

      


        public Boolean doCOMConnection(int port, string baudRateCom, string parityCom, int dataBitsCom, int stopBitsCom)
        {
            string portNumber = "COM" + port.ToString();
            bool responseInteger = false;
            serial = new SerialPort(portNumber, int.Parse(baudRateCom), (Parity)Enum.Parse(typeof(Parity), parityCom),
                                    dataBitsCom, (StopBits)Enum.Parse(typeof(StopBits), stopBitsCom.ToString()));
            serial.ReadTimeout = 2500000;
            serial.WriteTimeout = 1000000;
            try
            {
                serial.Open();
                responseInteger = true;
               
            }
            catch (IOException e)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());
                serial.Close();
            }
            ConfigData configData = new ConfigData();
            configData.commPortNumber = port;
            configData.connectionMode = "COM";
            SaveConfig(configData);

            return responseInteger;
           
        }
        //public int doCOMTransaction(int Port, string baudRateCom, string parityCom, int dataBitsCom, int stopBitsCom,string inputReqData,int transactionType, out string response)
        //{
        //    byte[] terminalResponse = new byte[50000];
        //    String[] resp1 = new String[500];
        //    String[] respsend = new String[500];
        //    string[] responseTemp = new string[1];
        //    string responseFinal = "";
        //    string reqbody = "";
        //    string portNumber = "COM" + Port;
        //    int responseInteger = 1;
        //    int resConnect = 1;
        //    string responseString = "";
        //    byte[] buffer = new byte[500];
        //    try
        //    {

        //        int resDisConnect = doCOMDisconnection();
        //        if (resDisConnect == 0)
        //        {
        //            //resConnect = doCOMConnection(Port, baudRateCom, parityCom, dataBitsCom, stopBitsCom);
        //        }

        //        if (resConnect == 0)
        //        {
        //            byte[] packResponse = new byte[150];
        //            TransactionRequest transactionRequest = JsonPackerParser.JsonParser<TransactionRequest>(inputReqData);
        //             reqbody = transactionRequest.requestBody;
        //            respsend = reqbody.Split(",");
                    
        //            string req = JsonConvert.SerializeObject(transactionRequest);

        //            byte[] requestData = Encoding.ASCII.GetBytes(req);
        //            //packing----------------------------------------------------------------------
        //            responseInteger = 2;
        //            serial.WriteTimeout = 5000;
        //            serial.Write(requestData, 0, requestData.Length);

        //            if (respsend[0] == "4001")
        //            {
        //                serial.ReadTimeout = 270000;
        //                int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
        //                responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        //            }
        //            responseFinal = responseString;
        //            responseInteger = 0;
                   
        //        }
        //    }
        //    catch (SocketException e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }
        //    response = responseFinal;
        //    return responseInteger;
        //}


        public string doTransaction(string inputReqBody,int transactionType,out string response)
        {
            byte[] terminalResponse = new byte[5000];
            String[] resp1 = new String[500];
            String[] respsend = new String[500];
            string[] responseTemp = new string[1];
            int resConnect = 1;
            string responseString = " ";
            string responseFinal = "";
            byte[] buffer = new byte[500];
            ConfigData configdata = getConfigData();
            string connctionMode = configdata.connectionMode;
            byte[] requestData = Encoding.ASCII.GetBytes(inputReqBody);

            try
            {


                if (connctionMode == "TCP/IP")
                {
                    
                    sock.SendTimeout = 27000;
                    int bytesSent = sock.Send(requestData);
                    byte[] responseData = new byte[5000];
                    sock.ReceiveTimeout = 27000;
                    int bytesReceived = sock.Receive(responseData);
                    responseString = Encoding.ASCII.GetString(responseData, 0, bytesReceived);
                }
                else
                {
                    serial.WriteTimeout = 5000;
                    serial.Write(requestData, 0, requestData.Length);
                    serial.ReadTimeout = 270000;
                    int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
                    responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
   
                }
                 
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
            }
            response = responseString;
            return response;

        }
       

        //public int doTCPIPTransaction(string IP, int PORT,string inputReqBody,int transcationType,out string response)
        // {
        //     byte[] terminalResponse = new byte[5000];
        //     String[] resp1 = new String[500];
        //     String[] respsend = new String[500];
        //     string[] responseTemp = new string[1];
        //    int resConnect = 1;
        //    string responseString = " ";
        //     string responseFinal = "";
        //     int responseInteger = 1;
        //     try
        //     {

        //        int resDisConnect = doTCPIPDisconnection();
        //        if (resDisConnect == 0)
        //        {
        //            resConnect = doTCPIPConnection(IP, PORT);
        //        }
        //        if ( resConnect== 0)
        //        {
        //            byte[] requestData = Encoding.ASCII.GetBytes(inputReqBody);
        //            sock.SendTimeout = 27000;
        //            int bytesSent = sock.Send(requestData);
        //            byte[] responseData = new byte[5000];
        //            sock.ReceiveTimeout = 27000;
        //            int bytesReceived = sock.Receive(responseData);
        //            responseString = Encoding.ASCII.GetString(responseData, 0, bytesReceived);
        //            responseInteger = 0;

        //        }
                
        //    }
        //     catch (SocketException e)
        //     {
        //         Console.WriteLine(e.ToString());
        //     }
        //    response = responseString;

        //    return responseInteger;
        // }


        public HashSet<ComTerminalResponse> scanSerialDevice()
        {
            Logger.Log("Auto COM Connection start.");
            ComJsonRequest req = new ComJsonRequest
            {
                posControllerId = "12345678",
                transactionType = "2",
                dateTime = "26042023181546",
                RFU1 = ""
            };
            Logger.Log("Auto COM Connection request" + req.ToString());
            HashSet<ComTerminalResponse> comterminalresponse = new HashSet<ComTerminalResponse>();
            ISet<string> allcomport = new HashSet<string>();
            allcomport = getDeviceManagerComPort();
            string[] comPorts = new string[allcomport.Count];

            int i = 0;
            foreach (string portString in allcomport)
            {
                string comPort = portString.Substring(portString.IndexOf("COM") + 3, portString.Length - portString.IndexOf("COM") - 4);
                comPorts[i++] = comPort;
            }
            string json = JsonConvert.SerializeObject(req);
            byte[] dataBytes = Encoding.UTF8.GetBytes(json);
            foreach (string comPort in comPorts)
            {
                string portcom = "COM" + comPort;
                SerialPort serial = new SerialPort(portcom, int.Parse(ComConstants.BAUDRATECOM), (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                                                    ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
                try
                {
                    serial.Open();
                    Logger.Log("Auto COM connection request send");
                    serial.Write(dataBytes, 0, dataBytes.Length);
                    System.Threading.Thread.Sleep(1000);
                    
                    // serial.WriteTimeout = 5000;
                    byte[] buffer = new byte[500];
                    serial.ReadTimeout = 5000;
                    int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
                    string responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Logger.Log("Auto COM Connection Terminal Response" + responseString);
                   
                    Console.WriteLine(responseString);
                    //ComTerminalResponse comresponse = JsonConvert.DeserializeObject<ComTerminalResponse>(responseString);
                    ComTerminalResponse comresponse = JsonPackerParser.JsonParser<ComTerminalResponse>(responseString);
                    if (responseString != null)
                    {
                        comresponse.COM = portcom;
                    }
                    comterminalresponse.Add(comresponse);

                    serial.Close();
                }
                catch (TimeoutException ex)
                {
                    Logger.Log("TimeoutExcetpion" + ex);
                    Console.WriteLine("TimeoutException occurred. Continuing with the next step...");
                }
                catch (SocketException se)
                {
                    Console.WriteLine("Connection Problem");
                   // MessageBox.Show("A socket error occurred: " + se.Message);
                }
                catch (IOException e)
                {
                    Console.WriteLine("Problem connecting to host");
                    Console.WriteLine(e.ToString());
                    serial.Close();
                }
            }
            return comterminalresponse;

        }
        private ISet<string> getDeviceManagerComPort()
        {
            ISet<String> allComport = new HashSet<string>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'");

            // Iterate through the search results
            foreach (ManagementObject obj in searcher.Get())
            {
                string caption = obj["Caption"].ToString();
                allComport.Add(caption);
            }
            return allComport;
        }

        public void DoAutoTCPIPConnection()
        {
            var client = new UdpClient();
            client.EnableBroadcast = true;

            // Specify the broadcast address and port number
            // var broadcastAddress = IPAddress.Broadcast;
            IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, 8888);
            // var port = PORT;
            try
            {
                string strHostName = "";
                strHostName = System.Net.Dns.GetHostName();

                IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);

                IPAddress[] addr = ipEntry.AddressList;

                string myIP = addr[addr.Length - 1].ToString();
                Console.WriteLine("My IP Address is :" + myIP);

                EcrTcpipRequest posData = new EcrTcpipRequest
                {
                    posControllerId = "12345678",
                    dateTime = "26042023181546",
                    transactionType = "1",
                    ecrIP = myIP,
                    ecrPort = 6666,
                    RFU1 = ""
                };
                // var ipAddress = IP;
                string json = JsonConvert.SerializeObject(posData);
                var message = Encoding.ASCII.GetBytes(json);
                while (true)
                {
                    // client.Send(message, message.Length, new IPEndPoint(broadcastAddress, port));
                    client.Send(message, message.Length, broadcastEndpoint);
                    Thread.Sleep(1000);


                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());

                client.Close();
            }
            

        }
        
        public List<TcpIpTerminalResponse> AutoTCPIPLintener()
        {
            TcpListener server = null;
            string json = null;
            List<TcpIpTerminalResponse> tcpIpTerminalResponse = new List<TcpIpTerminalResponse>();

            try
            {
                string strHostName = "";
                strHostName = System.Net.Dns.GetHostName();

                IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);

                IPAddress[] addr = ipEntry.AddressList;

                string myIP = addr[addr.Length - 1].ToString();
                int port = 6666;
                IPAddress localAddr = IPAddress.Parse(myIP);

                server = new TcpListener(localAddr, port);

                server.Start();

                Console.WriteLine("Waiting for connections...");

                // Start the timer to stop the server after 5 seconds
                Timer timer = new Timer(StopServer, null, 30000, Timeout.Infinite);

                // Wait for a connection request
                TcpClient client = server.AcceptTcpClient();
               
                
                NetworkStream stream = client.GetStream();

                // Read the incoming data
                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                

                json = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            TcpIpTerminalResponse tcpIpTerminalResponsejson = JsonConvert.DeserializeObject<TcpIpTerminalResponse>(json);
                tcpIpTerminalResponse.Add(tcpIpTerminalResponsejson);


                Console.WriteLine("Received: {0}", json);
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
            finally
            {
                server.Stop();
            }

            return tcpIpTerminalResponse; 

            // Stop the server and set json to null
            void StopServer(object state)
            {
                server.Stop();
                Console.WriteLine("Server stopped.");
               
            }


        }

        public Boolean isOnlineConnection(string IP, int PORT)
        {
            responseboolean = false;
            IPAddress host = IPAddress.Parse(IP);
            IPEndPoint hostep = new IPEndPoint(host, PORT);
            try
            {
              int resdisconnect  =  doTCPIPDisconnection();
                if (resdisconnect == 0)
                {
                    sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sock.Connect(hostep);
                    responseboolean = true;
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());
                responseboolean = false;
                sock.Close();
            }
            ConfigData configData = new ConfigData();
            configData.tcpIp = IP;
            configData.tcpPort = PORT;
            configData.connectionMode = "TCP/IP";

            SaveConfig(configData);
            return responseboolean;
        }



        public int doTCPIPConnection(string IP, int PORT)
        {
            int responseInteger = 1;
            IPAddress host = IPAddress.Parse(IP);
            IPEndPoint hostep = new IPEndPoint(host, PORT);
            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(hostep);
                responseInteger = 0;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());
                responseInteger = 1;
                sock.Close();
            }
            return responseInteger;
        }
        public int doTCPIPDisconnection()
        {
            int responseInteger = 1;
            try
            {
                sock.Close();
                responseInteger = 0;
            }
            catch (IOException e)
            {
                Console.WriteLine("Problem on disconnecting host");
                Console.WriteLine(e.ToString());

            }
            return responseInteger;
        }

        public int doCOMDisconnection()
        {
            int responseInteger = 1;
            try
            {
                serial.Close();
                responseInteger = 0;
            }
            catch (IOException e)
            {
                Console.WriteLine("Problem on disconnecting host");
                Console.WriteLine(e.ToString());

            }
            return responseInteger;
        }
       
      
       
      
       
       
       
    }


}
