using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
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
using System.Text.Json;
using Serilog;
using Serilog.Events;
using System.Security.Cryptography.X509Certificates;


namespace PosLibs.ECRLibrary.Service
{
    public class ConnectionService
    {

        public ConnectionService()
        {


        }

        // private static readonly ILogger logger = new LoggerConfiguration().CreateLogger();
        public static Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static SerialPort serial = new SerialPort();

        public string PortCom;
        public string IPortCom;
        public string bafudRateCom = string.Empty;
        public string parityCom = string.Empty;
        public int dataBitsCom;
        public int stopBitsCom;
        Boolean res = false;
        public ScanDeviceListener listener;
        public TransactionListener trasnlistener;
        public static bool isConnectivityFallbackAllowed;
        List<DeviceList> deviceLists = new List<DeviceList>();

        public static string[] connectionPriorityMode;

        public Boolean responseboolean;
        private Thread thread;
        public static string fullcomportName = "";
        ConfigData configdata = new ConfigData();
        public bool checkComConn()
        {
            bool status = serial.IsOpen;
            return status;

        }
        private const string PortSettingsFilePath = "C:\\PinLabs\\Configure.json";

        public ConfigData getConfigData()
        {
            
            if (File.Exists(PortSettingsFilePath))
            {
                string json = File.ReadAllText(PortSettingsFilePath);
                configdata = JsonConvert.DeserializeObject<ConfigData>(json);
                return configdata;
            }

            // Default port number if the file doesn't exist
            return configdata;
        }


        public void setConfiguration(ConfigData configData)
        {
            ConfigData portSettings = new ConfigData
            {
                tcpIp = configData.tcpIp,
                tcpPort = configData.tcpPort,
                commPortNumber = configData.commPortNumber,
                connectionMode = configData.connectionMode,
                communicationPriorityList = configData.communicationPriorityList,
                isConnectivityFallBackAllowed = configData.isConnectivityFallBackAllowed,
            };
            string json = JsonConvert.SerializeObject(portSettings);

            File.WriteAllText(PortSettingsFilePath, json);
        }

        public Boolean AutoConnect()
        {
            Boolean value = false;
            ConfigData configdata = getConfigData();
            if (configdata != null)
            {
                if (configdata.connectionMode == "TCP/IP")
                {
                    string tcpip = configdata.tcpIp.ToString();
                    int tcpport = configdata.tcpPort;
                    value = isOnlineConnection(tcpip, tcpport);
                }
                else if (configdata.connectionMode == "COM")
                {
                    int commport = configdata.commPortNumber;
                    value = isComDeviceConnected(commport);
                }
                else
                {
                    //MessageBox.Show("Please connect with Termianl");
                }


            }

            return value;

        }





        public Boolean isComDeviceConnected(int comPort)
        {

            bool responseInteger = false;
            var disconnect = doCOMDisconnection();
            if (disconnect == 0)
            {
                
                string portNumber = "COM" + comPort.ToString();

                 serial = new SerialPort(portNumber, int.Parse(ComConstants.BAUDRATECOM), (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                                                    ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
                try
                {
                   
                    serial.Open();
                    
                    
                    responseInteger = true;
                    configdata = getConfigData();
                    configdata.commPortNumber = comPort;
                    configdata.tcpIp = configdata.tcpIp;
                    configdata.tcpPort = configdata.tcpPort;
                    configdata.connectionMode = "COM";
                    configdata.isConnectivityFallBackAllowed = false;
                    setConfiguration(configdata);

                }
                catch (IOException e)
                {
                    Console.WriteLine("Problem connecting to host");
                    Console.WriteLine(e.ToString());
                    serial.Close();
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine("Unauthorized Access Exception");
                }


            }
            return responseInteger;

        }
        public T jsonParser<T>(string json)
        {
            T res = default(T);
            try
            {
                res = JsonConvert.DeserializeObject<T>(json);
                return res;

            }
            catch (JsonReaderException e)
            {
                Console.WriteLine("Unexcepted character");

            }

            return res;
        }
       

       

        public string checkComport(string port)
        {
            ISet<string> comallport = new HashSet<string>();
            string fullcomportName = "";
            comallport = getDeviceManagerComPort();
            if (comallport != null)
            {
                foreach (string portString in comallport)
                {
                    string comPort = portString.Substring(portString.IndexOf("COM") + 3, portString.Length - portString.IndexOf("COM") - 4);
                    string res = "COM" + comPort;
                    if (port == res)
                    {
                        fullcomportName = portString;
                        Console.WriteLine(portString);
                    }

                }

            }
            return fullcomportName;
        }
        public void scanSerialDevice(ScanDeviceListener scanDeviceListener)
        {
            Log.Information("Inside scanSerialDevice method");
            this.listener = scanDeviceListener;
            string responseString = "";
          
            string comreq = getComDeviceRequest();


            ISet<string> allcomport = new HashSet<string>();
            allcomport = getDeviceManagerComPort();
            if (allcomport.Count == 0)
            {
                listener.onFailure("Please Connect Terminal/USB Error", 1005);

            }
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
                 serial = new SerialPort(portcom, int.Parse(ComConstants.BAUDRATECOM), (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                                                    ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
                try
                {
                   
                    if (sendData(comreq))
                    {
                        responseString = serialrevData();

                    }
                    var disconnect = doCOMDisconnection();
                    if (disconnect == 0)
                    {
                        res = isComDeviceConnected(int.Parse(comPort));
                        Log.Information("isComDeviceConnected" + true);
                    }
                    fullcomportName = checkComport(portcom);
                    PortCom = fullcomportName;
                    if (responseString != "")
                    {
                        addToDeviceList(responseString);
                    }
                    serial.Close();

                }
                catch (SocketException se)
                {
                    Console.WriteLine("Connection Problem");

                }
                catch (TimeoutException ex)
                {
                    listener.onFailure("No Device Found", 1002);
                }
                catch (IOException io)
                {
                    listener.onFailure("Try Again", 1005);
                }

                catch (InvalidOperationException e)
                {
                    listener.onFailure("IOException", 1001);
                }
            }

            if (listener != null)
            {
                if (deviceLists != null && deviceLists.Count > 0)
                {
                    
                    listener.onSuccess(deviceLists);
                    
                }

            }
        }
        public bool sendData(string res)
        {
            if (serial.IsOpen)
            {
                serial.Close();
            }
            byte[] dataBytes = Encoding.UTF8.GetBytes(res);
            serial.Open();
            serial.Write(dataBytes, 0, dataBytes.Length);
            
            System.Threading.Thread.Sleep(1000);
            return true;
        }
        public bool sendCOMTXNData(string res)
        {
           
            byte[] dataBytes = Encoding.UTF8.GetBytes(res);
            serial.Write(dataBytes, 0, dataBytes.Length);

            System.Threading.Thread.Sleep(1000);
            return true;
        }
        public string serialrevData()
        {
            byte[] buffer = new byte[500];
            serial.ReadTimeout = 5000;
            int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
            string responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
          
            return responseString;
        }
        public string receiveCOMTxnrep()
        {
            
            byte[] buffer = new byte[500];
            serial.ReadTimeout = 80000;
            int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
            Log.Information("com response receive timeout:-" + 80000);
            string responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Log.Information("receive com txn response:" + responseString);
            if (serial.IsOpen)
            {
                serial.Close();
            }
            return responseString;

        }



        public void addToDeviceList(string recivebuff)
        {
            try
            {
                DeviceList deviceList = new DeviceList();

                TerminalConnectivityResponse terminalConnectivity = JsonConvert.DeserializeObject<TerminalConnectivityResponse>(recivebuff);
                deviceList.cashierId = terminalConnectivity.cashierId;
                deviceList.MerchantName = terminalConnectivity.MerchantName;
                deviceList.deviceId = terminalConnectivity.devId;
                deviceList.deviceIp = terminalConnectivity.posIP;
                deviceList.devicePort = terminalConnectivity.posPort;
                deviceList.msgType = terminalConnectivity.msgType;
                if (deviceList.msgType == 1)
                {
                    deviceList.connectionMode = "TCP IP";
                }
                else
                {
                    deviceList.connectionMode = "COM";
                }
                deviceList.SerialNo = terminalConnectivity.slNo;
                deviceList.COM = PortCom;

                deviceLists.Add(deviceList);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine("JsonReaderException ");
            }
        }
        public string getComDeviceRequest()
        {
            ComDeviceRequest req = new ComDeviceRequest
            {
                cashierId = "12345678",
                msgType = "2",
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
            Log.Information("Inside scanOnline Device mehtod");
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
                    cashierId = "12345678",
                    msgType = "1",
                    ecrIP = myIP,
                    ecrPort = "6000",
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
            bool isServerActive;



            string json = null;
            try
            {
                string strHostName = "";
                strHostName = System.Net.Dns.GetHostName();

                IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);

                IPAddress[] addr = ipEntry.AddressList;

                string myIP = addr[addr.Length - 1].ToString();
                int port = 6000;
                IPAddress localAddr = IPAddress.Parse(myIP);
                server = new TcpListener(localAddr, port);
                server.Start();
                Timer timer = new Timer(StopServer, null, 10000, Timeout.Infinite);
                isServerActive = server.Server?.IsBound ?? false;


                while (isServerActive ==true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    
                    NetworkStream stream = client.GetStream();
                    while (isServerActive == true)
                    {

                        byte[] buffer = new byte[256];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        json = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        break;

                    }
                    addToDeviceList(json);
                }

            }
            catch (SocketException ex)
            {

                Console.WriteLine("Network Error");
            }
            catch (IOException e)
            {
                Console.WriteLine("Failed to start server socket: " + e.Message);
            }
            finally
            {
                server.Stop();
                if (listener != null)
                {
                    if (deviceLists != null && deviceLists.Count > 0)
                    {
                        listener.onSuccess(deviceLists);
                    }
                    else
                    {
                        listener.onFailure("No Device Found Please Check NetWork", 1005);
                    }
                }

            }
            // Stop the server and set json to null
            void StopServer(object state)
            {
                server.Stop();
                isServerActive = false;
                Console.WriteLine("Server stopped.");

            }

        }
        public Boolean isOnlineConnection(string IP, int PORT)
        {
            Log.Information("Inside IsOnlineConnection method");
            responseboolean = false;
            IPAddress host = IPAddress.Parse(IP);
            IPEndPoint hostep = new IPEndPoint(host, PORT);
            try
            {
                int resdisconnect = doTCPIPDisconnection();
                if (resdisconnect == 0)
                {
                    sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sock.Connect(hostep);
                    responseboolean = true;
                    Log.Information("isOnline connected:" + responseboolean);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());
                responseboolean = false;
                Log.Information("isOnline connected:" + responseboolean);
                sock.Close();
            }

            configdata = getConfigData();
            configdata.tcpIp = IP;
            configdata.tcpPort = PORT;
            configdata.connectionMode = "TCP/IP";
            configdata.isConnectivityFallBackAllowed = false;
            configdata.commPortNumber = configdata.commPortNumber;

            setConfiguration(configdata);
            return responseboolean;
        }

        public bool sendTcpIpTxnData(string requestdata)
        {   
            bool responseboolen = false;
            if (requestdata != null)
            {
                try
                {
                    
                    byte[] requestData = Encoding.ASCII.GetBytes(requestdata);
                    sock.SendTimeout = 27000;
                    int bytesSent = sock.Send(requestData);
                    Console.WriteLine("Transaction RequestBody:" + requestdata);
                    responseboolen = true;
                    Log.Information("tcp/ip transaction data send successfully");
                }
                catch(SocketException e)
                {
                    Console.WriteLine("Socket is closed please try agian");
                    Log.Error("send tcp/Ip txn request failed");
                    responseboolen = false;
                }
            }
            return responseboolen;
        }
        public string  receiveTcpIpTxnData()
        {
            string responseString = "";
            byte[] responseData = new byte[5000];
            sock.ReceiveTimeout = 80000;
            Console.WriteLine("TCP/IP socket Receive Timeout:" + 18000);
            Log.Information("Txn response receive timeout" + 80000);
            int bytesReceived = sock.Receive(responseData);
            responseString = Encoding.ASCII.GetString(responseData, 0, bytesReceived);
            
            Console.WriteLine("Transaction Response:" + responseString);

            return responseString;

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
                Log.Information("is com port disconnected" + true);
                responseInteger = 0;
            }
            catch (IOException e)
            {
                Console.WriteLine("Problem on disconnecting host");
                Console.WriteLine(e.ToString());
                Log.Error("Com port Disconnected failed");

            }
            return responseInteger;
        }
       

        



    }




}
