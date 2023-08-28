
using System.IO.Ports;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using PosLibs.ECRLibrary.Common;
using PosLibs.ECRLibrary.Model;
using Serilog;
using PosLibs.ECRLibrary.Common.Interface;
using System.Net.NetworkInformation;
namespace PosLibs.ECRLibrary.Service
{
    public class ConnectionService
    {
        public ConnectionService() { }
        private static  Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static SerialPort serial = new SerialPort();
        private string PortCom = string.Empty;
       
        private IScanDeviceListener? listener;
        public readonly static bool isConnectivityFallbackAllowed;
        readonly List<DeviceList> deviceLists = new List<DeviceList>();
        private string fullcomportName = string.Empty;
        ConfigData configdata = new ConfigData();
        private bool isConnected = false;
        public bool checkComConn()
        {
            bool status = serial.IsOpen;
            return status;

        }
        private const string PortSettingsFilePath = PinLabsEcrConstant.FILE_PATH;


        /// <summary>
        /// fetch connection details data like tcp/ip,com number,connectionMode,pariritoy list
        /// </summary>
        /// <returns></returns>
        public ConfigData? GetConfigData()
        {

            if (File.Exists(PortSettingsFilePath))
            {
                string json = File.ReadAllText(PortSettingsFilePath);
                configdata = JsonConvert.DeserializeObject<ConfigData>(json);
                if (configdata != null)
                {
                    return configdata;
                }
            }
            return configdata;
        }
        /// <summary>
        /// set all the connection 
        /// </summary>
        /// <param name="configData"></param>
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
                CashierID = configData.CashierID,
                CashierName = configData.CashierName,
                retrivalcount = configdata.retrivalcount,
                connectionTimeOut = configData.connectionTimeOut,
                tcpIpaddress = configData.tcpIpaddress,
                comfullName = configData.comfullName,
                comserialNumber = configData.comserialNumber,
                tcpIpPort = configData.tcpIpPort,
                tcpIpDeviceId = configData.tcpIpDeviceId,
                tcpIpSerialNumber = configData.tcpIpSerialNumber,
                comDeviceId = configData.comDeviceId,
                LogPath = configData.LogPath,
                loglevel = configData.loglevel,
                isComHeartActive = configData.isComHeartActive,

                

            
            };
            string json = JsonConvert.SerializeObject(portSettings);

            File.WriteAllText(PortSettingsFilePath, json);
        }
        /// <summary>
        /// This method is used to autoconnectivity when system short down and agian start its automatically connected with privious connection
        /// </summary>
        /// <returns></returns>
        public Boolean AutoConnect()
        {
            Boolean value = false;
            ConfigData? fetchData = GetConfigData();
            if (fetchData != null)
            {
                if (fetchData.connectionMode == PinLabsEcrConstant.TCPIP)
                {
                    string tcpip = fetchData.tcpIp.ToString();
                    int tcpport = fetchData.tcpPort;
                    value = IsOnlineTest(tcpip, tcpport);
                }
                else if (fetchData.connectionMode == PinLabsEcrConstant.COM)
                {
                    int commport = fetchData.commPortNumber;
                    value = IsComDeviceConnected(commport);
                }
                else
                {
                    Log.Error("AutoConnect Fail");
                }

            }
            return value;
        }
        /// <summary>
        /// this method used to connect the pax device though serial com port
        /// </summary>
        /// <param name="comPort"></param>
        /// <returns></returns>
        public Boolean IsComDeviceConnected(int comPort)
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
                    configdata = GetConfigData();
                    if (configdata != null)
                    {
                        configdata.commPortNumber = comPort;
                        this.configdata.tcpIp = configdata.tcpIp;
                        this.configdata.tcpPort = configdata.tcpPort;
                        configdata.isComHeartActive = true;
                        configdata.connectionMode = PinLabsEcrConstant.COM;
                        setConfiguration(configdata);
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("Problem connecting to host");
                    Console.WriteLine(e.ToString());
                    serial.Close();
                    serial.Dispose();
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine("Unauthorized Access Exception"+e);
                }
            }
            return responseInteger;
        }
        public T? jsonParser<T>(string json)
        {
            T? res = default;
            try
            {
                res = JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine("Unexcepted character" + e);

            }
            return res;
        }
        /// <summary>
        /// through third party API this method fetch all the COM port those are connected in system
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public string checkComport(string port)
        {
            ISet<string> comallport = getDeviceManagerComPort();
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
        /// <summary>
        /// this method is used to fetch all the com port from POS bride
        /// </summary>
        /// <param name="scanDeviceListener"></param>
        public void ScanSerialDevice(IScanDeviceListener scanDeviceListener)
        {
            Log.Information("Inside scanSerialDevice method");
            this.listener = scanDeviceListener;
            string responseString = string.Empty;
            string comreq = getComDeviceRequest();
            ISet<string> allcomport = getDeviceManagerComPort();
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
                    SendData(comreq);
                    responseString = SerialrevData();
                    if (SendData(comreq))
                    {
                        responseString = SerialrevData();
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
                    Console.WriteLine("Connection Problem"+se);

                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine("Connection Problem" + ex);
                    listener.onFailure("No Device Found", 1002);
                }
                catch (IOException io)
                {
                    Console.WriteLine("Connection Problem" + io);
                    listener.onFailure("Try Again", 1005);
                }

                catch (InvalidOperationException e)
                {
                    Console.WriteLine("InvalidOperationException" + e);
                    listener.onFailure("IOException", 1001);
                }
            }
            if (deviceLists != null && deviceLists.Count > 0)
            {
              listener.onSuccess(deviceLists);
            }
        }


        /// <summary>
        /// The TerminalConnectionChecker method sets up a timer-based mechanism for regularly monitoring the status of terminal connections. It creates an instance of the Timer class, configuring it to execute the ConnectionCheckTimer_Tick method at intervals of 10 seconds
        /// </summary>
        public void TerminalConnectionChecker()
        {
            Timer connectionCheckTimer = new Timer(ConnectionCheckTimer_Tick, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// The purpose of this method is to provide a scheduled mechanism for monitoring the health of both COM port and TCP/IP connections.
        /// </summary>
        /// <param name="state"></param>
        private void ConnectionCheckTimer_Tick(object state)
        {
            CheckCOMHeartBeat();
            CheckTcpIpHeartBeat();
        }
        /// <summary>
        /// The CheckTcpIpHeartBeat method continuously evaluates the status of a TCP/IP terminal's connectivity by repeatedly attempting to ping its associated IP address. It utilizes the IsHostReachable method to check if the terminal is reachable
        /// </summary>
        /// <returns></returns>
        public string CheckTcpIpHeartBeat()
        {
            while (true)
            {
                bool isTerminalReachable = IsHostReachable(configdata.tcpIp);
                if (isTerminalReachable)
                {
                    return PinLabsEcrConstant.TCPIPHEALTHACTIVE;
                }
                else
                {
                    return PinLabsEcrConstant.TCPIPHEALTHINACTIVE;
                }
            }
        }
        /// <summary>
        /// The IsHostReachable method is responsible for determining whether a remote host, identified by its IP address, is reachable or not using the ICMP protocol (commonly associated with the "ping" command). The method sends an ICMP echo request to the specified host IP and waits for a response to assess its reachability
        /// </summary>
        /// <param name="hostIP"></param>
        /// <returns></returns>
        private bool IsHostReachable(string hostIP)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(hostIP, 1000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (PingException)
            {
                return false;
            }
        }
        /// <summary>
        /// The CheckCOMHeartBeat method is designed to determine the status of a communication connection (likely a COM port) and provide a status code based on the result of the connection check
        /// </summary>
        /// <returns></returns>
        public string CheckCOMHeartBeat()
        {
            bool status = checkComConn();
            if (status)
            {
                return PinLabsEcrConstant.COMHEALTHACTIVE;
            }
            else
            {
                return PinLabsEcrConstant.COMHEALTHINACTIVE;     
            }  
        }
        /// <summary>
        /// Sends the specified COM request data to the POS Bridge.
        /// </summary>
        /// <param name="res">The data to be sent.</param>
        /// <returns>True if the data sending is successful; otherwise, false.</returns>
        public bool SendData(string res)
        {
            if (serial.IsOpen)
            {
                serial.Close();
            }
            byte[] dataBytes = Encoding.UTF8.GetBytes(res);
            serial.Open();
            serial.Write(dataBytes, 0, dataBytes.Length);
            Thread.Sleep(1000);
            return true;
        }
        /// <summary>
        /// Receives and returns COM details from the serial communication.
        /// </summary>
        /// <returns>The received COM details as a string.</returns>
        public string SerialrevData()
        {
            byte[] buffer = new byte[500];
            serial.ReadTimeout = 5000;
            int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
            string responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            return responseString;
        }
        /// <summary>
        /// this method used to receive the COM Txn response
        /// </summary>
        /// <returns></returns>
        public string ReceiveCOMTxnrep()
        {
            string responseString = string.Empty;
            try
            {
                byte[] buffer = new byte[6000];
        
                int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
                responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);  
            }
            catch(InvalidOperationException ex)
            {
                Console.WriteLine(ex.ToString());
                serial.Close();
                serial.Dispose();
                Thread.Sleep(1000);
                IsComDeviceConnected(configdata.commPortNumber);
            }
            return responseString;
        }
        /// <summary>
        /// this method used to show list of  devices those are connected with com and wifi 
        /// </summary>
        /// <param name="recivebuff"></param>
        public void addToDeviceList(string recivebuff)
        {
            try
            {
                DeviceList deviceList = new DeviceList();

                TerminalConnectivityResponse terminalConnectivity = JsonConvert.DeserializeObject<TerminalConnectivityResponse>(recivebuff);
                if (terminalConnectivity != null)
                {
                    deviceList.cashierId = terminalConnectivity.cashierId;
                    deviceList.MerchantName = terminalConnectivity.MerchantName;
                    deviceList.deviceId = terminalConnectivity.devId;
                    deviceList.deviceIp = terminalConnectivity.posIP;
                    deviceList.devicePort = terminalConnectivity.posPort;
                    deviceList.msgType = terminalConnectivity.msgType;
                    if (deviceList.msgType == 1)
                    {
                        deviceList.connectionMode = PinLabsEcrConstant.TCPIP;
                    }
                    else
                    {
                        deviceList.connectionMode = PinLabsEcrConstant.COM;
                    }
                    deviceList.SerialNo = terminalConnectivity.slNo;
                    deviceList.COM = PortCom;
                }

                deviceLists.Add(deviceList);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine("JsonReaderException " + ex);
            }
        }
        //com connection request Data
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
        public ISet<string> getDeviceManagerComPort()
        {
            ISet<String> allComport = new HashSet<string>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'");


            foreach (ManagementBaseObject obj in searcher.Get())
            {
                string caption = obj["Caption"].ToString();
                allComport.Add(caption);
            }
            return allComport;
        }
        /// <summary>
        /// this method boardcast a message including locally ip and port number ,  pos bride accept board cast message and send device ip and port number to ECR 
        /// </summary>
        /// <param name="scanDeviceListener"></param>
        public void scanOnlineDevice(IScanDeviceListener scanDeviceListener)
        {
            Log.Information("Inside scanOnline Device mehtod");
            this.listener = scanDeviceListener;
            var client = new UdpClient();
            client.EnableBroadcast = true;
            IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, 8888);
            try
            {
                string strHostName = "";
                string myIP = string.Empty;
                strHostName = System.Net.Dns.GetHostName();

                IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);

                IPAddress[] addr = ipEntry.AddressList;

                myIP = addr[addr.Length - 1].ToString();
                Console.WriteLine("My IP Address is :" + myIP);

                EcrTcpipRequest posData = new EcrTcpipRequest
                {
                    cashierId = "12345678",
                    msgType = "1",
                    ecrIP = myIP,
                    ecrPort = "6666",
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
                    listener.onFailure(PinLabsEcrConstant.IO_EXC_MSG, PinLabsEcrConstant.IOEXCEPTION);
                    Log.Error("IOException" + e);
                }
            }
        }
        //this method listen all the incoming request form posbride
        private void TcpListen()
        {
            Thread thread = new Thread(Run);
            thread.Priority = ThreadPriority.Normal;
            thread.Start();
        }
        //inside this method accept all the device those are availabe in same network
        public void Run()
        {
            TcpListener server = null;
            bool isServerActive;
            string json=string.Empty;
            try
            {
                string strHostName = "";
                strHostName = System.Net.Dns.GetHostName();
                IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);
                IPAddress[] addr = ipEntry.AddressList;
                string myIP = addr[addr.Length - 1].ToString();
                int port = ComConstants.PORT;
                IPAddress localAddr = IPAddress.Parse(myIP);
                server = new TcpListener(localAddr, port);
                server.Start();
                Timer timer = new Timer(StopServer, null, 10000, Timeout.Infinite);
                isServerActive = server.Server?.IsBound ?? false;
                while (isServerActive)
                {
                    TcpClient client = server.AcceptTcpClient();

                    NetworkStream stream = client.GetStream();
                    if (isServerActive)
                    {
                        byte[] buffer = new byte[256];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        json = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    }
                    //add all tcp/ip pax device
                    addToDeviceList(json);
                }
            }
            catch (SocketException ex)
            {

                Log.Error("Socket Exception" + ex);
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
                        listener.onFailure("No Device Found Please Check NetWork", PinLabsEcrConstant.NO_DEV_FOUND);
                    }
                }
            }
            void StopServer(object state)
            {
                Log.Debug("Inside Stop Server Method");
                server.Stop();
                isServerActive = false;
                Log.Information("Server stopped");
            }
        }

        public Boolean IsOnlineTest(string IP, int PORT)
        {
            Log.Debug("Inside IsOnlineConnection method");
            bool responseboolean = false;
            IPAddress host = IPAddress.Parse(IP);
            IPEndPoint hostep = new IPEndPoint(host, PORT);
            try
            {
                int resdisconnect = doTCPIPDisconnection();
                if (resdisconnect == 0)
                {
                    sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sock.Connect(hostep);
                    Log.Information("Connected IP:" + IP);
                    Log.Information("Connexted POrt:" + PORT);
                    isConnected = true;
                    Log.Information("isOnline connected:" + responseboolean);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());
                isConnected = false;
                Log.Information("isOnline connected:" + responseboolean);
                
                sock.Close();
            }
            if (configdata != null)
            {
                configdata.tcpIp = IP;
                configdata.tcpPort = PORT;
                configdata.connectionMode = PinLabsEcrConstant.TCPIP;
                this.configdata.commPortNumber = configdata.commPortNumber;
                setConfiguration(configdata);
            }

            return isConnected;
        }
        
        /// <summary>
        /// this accept the the IP address and Port number and make a online connection like tcp/ip connection 
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="PORT"></param>
        /// <returns></returns>
        public Boolean IsOnlineConnection(string IP, int PORT)
        {
            Log.Debug("Inside IsOnlineConnection method");
            bool responseboolean = false;
            IPAddress host = IPAddress.Parse(IP);
            IPEndPoint hostep = new IPEndPoint(host, PORT);
            try
            {
                int resdisconnect = doTCPIPDisconnection();
                if (resdisconnect == 0)
                {
                    int maxRetries = int.Parse(configdata.retrivalcount);
                    int connectionTimeoutMillis = int.Parse(configdata.connectionTimeOut);
                    sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    for (int retry = 1; retry <= maxRetries; retry++)
                    {
                        try
                        {
                            IAsyncResult result = sock.BeginConnect(hostep, null, null);
                            bool success = result.AsyncWaitHandle.WaitOne(connectionTimeoutMillis*1000);
                            if (success)
                            {
                                sock.EndConnect(result);
                                Log.Information("Connected IP:" + IP);
                                Log.Information("Connexted Port:" + PORT);
                                Log.Information("Connection Time Out :" + connectionTimeoutMillis*1000);
                                Log.Information("Connection retry " + maxRetries);
                                responseboolean = true;
                                Log.Information("isOnline connected:" + responseboolean);
                                break;
                            }
                        }
                        catch(SocketException e)
                        {
                            if (retry < maxRetries)
                            {
                                try
                                {
                                    Thread.Sleep(1000);
                                }
                                catch (ThreadInterruptedException ie)
                                {
                                    Thread.CurrentThread.Abort();
                                    Console.WriteLine(ie.ToString());
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
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
            if (configdata != null)
            {
                configdata.tcpIp = IP;
                configdata.tcpPort = PORT;
                configdata.connectionMode = PinLabsEcrConstant.TCPIP;
                this.configdata.retrivalcount = configdata.retrivalcount;
                this.configdata.connectionTimeOut = configdata.connectionTimeOut;
                this.configdata.commPortNumber = configdata.commPortNumber;
                setConfiguration(configdata);
            }
            return responseboolean;
        }
        /// <summary>
        /// this method accept the tcp/ip txn request and send to pos bride
        /// </summary>
        /// <param name="requestdata"></param>
        /// <returns></returns>
        public bool SendTcpIpTxnData(string requestdata)
        {
            bool responseboolen = false;
            if (requestdata != null)
            {
                try
                {
                    byte[] requestData = Encoding.ASCII.GetBytes(requestdata);
                    sock.SendTimeout = 27000;
                    sock.Send(requestData);
                    Console.WriteLine("Transaction RequestBody:" + requestdata);
                    responseboolen = true;
                    Log.Information("tcp/ip transaction data send successfully");
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Socket is closed please try agian");
                    Log.Error("send tcp/Ip txn request failed" + e);
                    responseboolen = false;
                }
            }
            return responseboolen;
        }

        /// <summary>
        /// This method receive the tcp/ip txn response
        /// </summary>
        /// <returns></returns>
        public string ReceiveTcpIpTxnData()
        {
            
            byte[] responseData = new byte[6000];
            int bytesReceived = sock.Receive(responseData);
            string responseString = Encoding.ASCII.GetString(responseData, 0, bytesReceived);
            Console.WriteLine("Transaction Response:" + responseString);
            sock.Close();
            return responseString;
        }
        /// <summary>
        /// This method work to disconnect the tcp/ip connection
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// this method is disconnect the com connection
        /// </summary>
        /// <returns></returns>
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