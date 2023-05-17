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
        public ScanDeviceListener listener;
        public TransactionListener trasnlistener;
        public static bool isConnectivityFallbackAllowed;
        List<DeviceList> deviceLists = new List<DeviceList>();

        public static string[] connectionPriorityMode;

        public Boolean responseboolean;
        private Thread thread;

        public bool checkComConn()
        {
            bool status = serial.IsOpen;
            return status;

        }
        private const string PortSettingsFilePath = "C:\\PinLabs\\Configure.json";

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
                tcpPort=configData.tcpPort,commPortNumber=configData.commPortNumber,connectionMode=configData.connectionMode,
                communicationPriorityList=configData.communicationPriorityList,
            };
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
            bool responseInteger = false;
            var disconnect = doCOMDisconnection();
            if (disconnect == 0)
            {
                string portNumber = "COM" + port.ToString();
                
                serial = new SerialPort(portNumber, int.Parse(baudRateCom), (Parity)Enum.Parse(typeof(Parity), parityCom),
                                        dataBitsCom, (StopBits)Enum.Parse(typeof(StopBits), stopBitsCom.ToString()));
                serial.ReadTimeout = 2500000;
                serial.WriteTimeout = 1000000;
                try
                {
                    serial.Open();
                    responseInteger = true;
                    ConfigData configData = new ConfigData();
                    configData.commPortNumber = port;
                    configData.connectionMode = "COM";
                    SaveConfig(configData);

                }
                catch (IOException e)
                {
                    Console.WriteLine("Problem connecting to host");
                    Console.WriteLine(e.ToString());
                    serial.Close();
                }
               
               
            }
            return responseInteger;

        }
        

        public string doTransaction(string inputReqBody,int transactionType, TransactionListener transactionListener,out string response)
        {
            this.trasnlistener = transactionListener;
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
                if (isConnectivityFallbackAllowed == true)
                {
                    bool transactionSuccessful = false;
                    for (int i = 0; i < connectionPriorityMode.Length; i++)
                    {
                        if (connectionPriorityMode[i] == "TCP/IP" || connectionPriorityMode[i] == "COM")
                        {
                            try
                            {
                                if (connectionPriorityMode[i] == "TCP/IP")
                                {
                                    sock.SendTimeout = 27000;
                                    int bytesSent = sock.Send(requestData);
                                    byte[] responseData = new byte[5000];
                                    sock.ReceiveTimeout = 27000;
                                    int bytesReceived = sock.Receive(responseData);
                                    responseString = Encoding.ASCII.GetString(responseData, 0, bytesReceived);
                                    transactionSuccessful = true;
                                }
                                else if (connectionPriorityMode[i] == "COM")
                                {
                                    try
                                    {
                                        serial.WriteTimeout = 5000;
                                        serial.Write(requestData, 0, requestData.Length);
                                        serial.ReadTimeout = 270000;
                                        int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
                                        responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                                        // If COM transaction succeeds, set transactionSuccessful to true
                                        transactionSuccessful = true;
                                    }
                                    catch(SocketException)
                                    {
                                        transactionSuccessful = false;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // Handle the InvalidOperationException here, if needed
                                        // You can log the exception or perform any specific actions
                                        // After handling the exception, set transactionSuccessful to false
                                        transactionSuccessful = false;
                                    }
                                }
                            }
                            catch (SocketException)
                            {
                                // Handle the SocketException here, if needed
                                // You can log the exception or perform any specific actions
                                // After handling the exception, the code will continue to the else block
                                transactionSuccessful = false;
                            }

                            if (transactionSuccessful)
                            {
                                // Transaction succeeded, break the loop
                                break;
                            }
                        }
                    }


                }
                else
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
                 
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }
            response = responseString;
            return response;

        }


        public void scanSerialDevice(ScanDeviceListener scanDeviceListener)
        {
            this.listener = scanDeviceListener;
            Logger.Log("Auto COM Connection start.");
            string comreq = getComDeviceRequest();

            ISet<string> allcomport = new HashSet<string>();
            allcomport = getDeviceManagerComPort();
            string[] comPorts = new string[allcomport.Count];

            if (serial.IsOpen)
            {
                serial.Close();
            }

            int i = 0;
            foreach (string portString in allcomport)
            {
                string comPort = portString.Substring(portString.IndexOf("COM") + 3, portString.Length - portString.IndexOf("COM") - 4);
                comPorts[i++] = comPort;
            }

            byte[] dataBytes = Encoding.UTF8.GetBytes(comreq);
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

                    byte[] buffer = new byte[500];
                    serial.ReadTimeout = 5000;
                    int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
                    string responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    PortCom = portcom;
                    addToDeviceList(responseString);

                 
                    serial.Close();
                }
                catch (SocketException se)
                {
                    Console.WriteLine("Connection Problem");
                    
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine("Timeout occurred");
                }
            }

            if (listener != null)
            {
                if (deviceLists != null && deviceLists.Count > 0)
                {
                    listener.onSuccess(deviceLists);
                }
                else
                {
                    listener.onFailure("IOException", 1001);
                }
            }
        }

        


        public void addToDeviceList(string recivebuff)
        {

            DeviceList deviceList = new DeviceList();

            TerminalConnectivityResponse terminalConnectivity = JsonConvert.DeserializeObject<TerminalConnectivityResponse>(recivebuff);
            deviceList.tid = terminalConnectivity.posControllerId;
            deviceList.MerchantName = terminalConnectivity.MerchantName;
            deviceList.deviceIp = terminalConnectivity.posIP;
            deviceList.devicePort = terminalConnectivity.posPort;
            deviceList.COM = PortCom;

            deviceLists.Add(deviceList);
        }
        public string getComDeviceRequest()
        {
            ComDeviceRequest req = new ComDeviceRequest
            {
                posControllerId = "12345678",
                transactionType = "2",
                dateTime = "26042023181546",
                RFU1 = ""
            };
            string jsonrequest = JsonConvert.SerializeObject(req);
            return jsonrequest;
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
        public void scanOnlineDevice(ScanDeviceListener scanDeviceListener)
        {
            this.listener = scanDeviceListener;
            var client = new UdpClient();
            client.EnableBroadcast = true;
            IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, 8888);

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
                string json = JsonConvert.SerializeObject(posData);
                var message = Encoding.ASCII.GetBytes(json);
                client.Send(message, message.Length, broadcastEndpoint);
                if (client.Client.Connected)
                {
                    client.Close();
                }
                TcpListen();
            }
            catch (IOException e)
            {
                if (listener != null)
                {
                    listener.onFailure("IOException", 1001);
                }
            }
        }

        private void TcpListen()
        {
            thread = new Thread(Run);
            thread.Priority = ThreadPriority.Normal;
            thread.Start();
        }

        public void Run()
        {
            TcpListener server = null;
          

            string json = null;
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
                Timer timer = new Timer(StopServer, null, 30000, Timeout.Infinite);

                // Wait for a connection request
                TcpClient client = server.AcceptTcpClient();


                NetworkStream stream = client.GetStream();

                // Read the incoming data
                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);


                json = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                addToDeviceList(json);
            }
            catch (SocketException ex)
            {

                if (listener != null)
                {
                    listener.onFailure("IOException", 1001);
                }

            }
            catch (IOException e)
            {
                Console.WriteLine("Failed to start server socket: " + e.Message);
            }
            finally
            {
                server.Stop();
            }
            // Stop the server and set json to null
            void StopServer(object state)
            {
                server.Stop();
                Console.WriteLine("Server stopped.");

            }
            if (listener != null)
            {
                if (deviceLists != null && deviceLists.Count > 0)
                {
                    listener.onSuccess(deviceLists);
                }
                else
                {
                    listener.onFailure("IOException", 1001);
                }
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
