
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
using System.Runtime.CompilerServices;

namespace PosLibs.ECRLibrary.Service
{
    public class ConnectionService
    {
       
        public ConnectionService() {
        }
        private static Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static SerialPort serial = new SerialPort();
        private string PortCom = string.Empty;
        private IScanDeviceListener? listener;
        public IConnectionListener _connlistener = new ConnectionListener();
        public readonly static bool isConnectivityFallbackAllowed;
        readonly List<DeviceList> deviceLists = new List<DeviceList>();
        private string fullcomportName = string.Empty;
        ConfigData configdata = new ConfigData();
        private bool isConnected = false;

        private const string PortSettingsFilePath = PosLibConstant.FILE_PATH;
        private string ReadSettingsFilePath = "C:\\POSLIBS\\Configure.json";
        public void setConfiguration(ConfigData configData)
        {
            Log.Debug("Enter setConfiguration()");
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
                isAppidle = configdata.isAppidle,
            };
            if (!Directory.Exists(PortSettingsFilePath))
            {
                Directory.CreateDirectory(PortSettingsFilePath);
            }
            string fileName = PosLibConstant.FILE_NAME;
            string filePath = Path.Combine(PortSettingsFilePath, fileName);
            ReadSettingsFilePath = filePath;
            string json = JsonConvert.SerializeObject(configData);
            try
            {
                File.WriteAllText(filePath, json);
            }
            catch(FileLoadException ex)
            {
                Log.Error("FileLoad Exception" + ex);
                Console.WriteLine(ex.Message);
            }
        }
        public ConfigData? GetConfigData()
        {
            Log.Debug("Enter GetConfigData()");

            if (File.Exists(ReadSettingsFilePath))
            {
                string json = File.ReadAllText(ReadSettingsFilePath);
                configdata = JsonConvert.DeserializeObject<ConfigData>(json);
                if (configdata != null)
                {
                    return configdata;
                }
            }
            return configdata;
        }
        public string CheckTcpIpHeartBeat()
        {
            Log.Debug("Enter CheckTcpIpHeartBeat()");
            bool isTerminalReachable;
            while (true)
            {
                if (configdata.tcpIp != "")
                {
                    isTerminalReachable = IsHostReachable(configdata.tcpIp);
                    if (isTerminalReachable)
                    {
                        return PosLibConstant.TCPIPHEALTHACTIVE;
                    }
                    else
                    {
                        return PosLibConstant.TCPIPHEALTHINACTIVE;
                    }
                }
                else
                { 
                    return PosLibConstant.TCPIPHEALTHINACTIVE;
                }
            }
        }
        private bool IsHostReachable(string hostIP)
        {
            if (configdata.isAppidle)
            {
                Log.Information("IsAppidle" + configdata.isAppidle);
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
            Log.Information("IsAppidle:-" + configdata.isAppidle);
            return false;
        }
        public string CheckCOMHeartBeat()
        {
            if (configdata.isAppidle)
            {
                bool status = CheckBothConnection();
                if (status)
                {
                    return PosLibConstant.COMHEALTHACTIVE;
                }
                else
                {
                    return PosLibConstant.COMHEALTHINACTIVE;
                }
            }
            return PosLibConstant.COMHEALTHINACTIVE;
        }
        public bool CheckBothConnection()
        {
            if (configdata.commPortNumber != 0 && configdata.isComHeartActive == configdata.commPortNumber)
            {
                if (CheckComStatus(configdata.commPortNumber, _connlistener))
                {
                    return true;
                }
            }
            return false;
        }
        public bool checkComConn()
        {
            if (serial != null)
            {
                return serial.IsOpen;
            }
            return false;

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
        public void ScanSerialDevice(IScanDeviceListener scanDeviceListener)
        {
            Log.Debug("Enter scanSerialDevice method");
            this.listener = scanDeviceListener;
            ISet<string> allUSBcomport = getDeviceManagerComPort();
            ISet<string> validComports = FilterValidComports(allUSBcomport);

            if (validComports.Count == 0)
            {
                listener.onFailure("Please Connect Terminal/USB Error", 1005);
                return;
            }
            string[] comPorts = GetComPorts(validComports);
            foreach (string comPort in comPorts)
            {
                try
                {
                    string portcom = "COM" + comPort;
                    InitializeSerialPort(portcom);
                    if (SendSerialDiscoveryData(GetComDeviceRequest()))
                    {
                        string responseString = SerialrevData();
                        Log.Information("Receive COM Discovery Response:" + responseString);
                        fullcomportName = checkComport(portcom);
                        PortCom = fullcomportName;

                        if (!string.IsNullOrEmpty(responseString))
                        {
                            AddToDeviceList(responseString);
                        }
                    }
                }
                catch (SocketException se)
                {
                    Log.Error(ExceptionConst.SOCKETEXCEPTION + se);
                }
                catch (TimeoutException ex)
                {
                    HandleTimeoutException(ex);
                    Log.Error(ExceptionConst.TIMEOUTEXCEPTION + ex);
                }
                catch (IOException io)
                {
                    HandleIOException(io);
                    Log.Error(ExceptionConst.IOEXCEPTION + io);
                }
                catch (InvalidOperationException e)
                {
                    HandleInvalidOperationException(e);
                    Log.Error(ExceptionConst.INVALIDOPERATIONEXCEPTION + e);
                }
                finally
                {
                    if (serial.IsOpen)
                    {
                        serial.Close();
                    }
                    serial.Dispose();
                    Thread.Sleep(1000);
                }
            }

            if (deviceLists != null && deviceLists.Count > 0)
            {
                listener.onSuccess(deviceLists);
                Log.Information("Scan Serial Discovery Done");
            }
        }
        private ISet<string> FilterValidComports(ISet<string> allUSBcomport)
        {
            HashSet<string> domainWords = new HashSet<string> { "Daemon" };
            ISet<string> validComports = new HashSet<string>();
            foreach (string comport in allUSBcomport)
            {
                if (!domainWords.Any(domainWord => comport.Contains(domainWord)))
                {
                    validComports.Add(comport);
                }
            }
            return validComports;
        }
        private string[] GetComPorts(ISet<string> validComports)
        {
            string[] comPorts = new string[validComports.Count];
            int i = 0;

            foreach (string portString in validComports)
            {
                string comPort = portString.Substring(portString.IndexOf("COM") + 3, portString.Length - portString.IndexOf("COM") - 4);
                comPorts[i++] = comPort;
            }

            return comPorts;
        }
        private void InitializeSerialPort(string portcom)
        {
            if (serial.IsOpen)
            {
                serial.Close();
                serial.Dispose();
            }
            serial = new SerialPort(portcom, int.Parse(ComConstants.BAUDRATECOM), (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                                                  ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
        }
        private void HandleTimeoutException(TimeoutException ex)
        {
            serial.Close();
            serial.Dispose();
            Console.WriteLine("Connection Problem" + ex);
            listener.onFailure("No Device Found", 1002);
        }
        private void HandleIOException(IOException io)
        {
            serial.Close();
            serial.Dispose();
            configdata.isAppidle = true;
            setConfiguration(configdata);
            Console.WriteLine("Connection Problem" + io);
            listener.onFailure("Try Again", 1005);
        }
        private void HandleInvalidOperationException(InvalidOperationException e)
        {
            Console.WriteLine("InvalidOperationException" + e);
            listener.onFailure("IOException", 1001);
        }       
        public bool SendSerialDiscoveryData(string res)
        {
            if (serial.IsOpen)
            {
                serial.Close();
                serial.Dispose();
            }
            byte[] dataBytes = Encoding.UTF8.GetBytes(res);
            serial.Open();
            serial.WriteTimeout = 3000;
            serial.Write(dataBytes, 0, dataBytes.Length);
            Log.Information("Send Com discovery req successfully");
            return true;
        }
        public string SerialrevData()
        {
            configdata.isAppidle = false;
            byte[] buffer = new byte[5000];
            Thread.Sleep(6000);
            int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
            string responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            serial.Close();
            return responseString;
        }
        public Boolean IsComDeviceConnected(int comPort, IConnectionListener icomlistener)
        {
            _connlistener = icomlistener;
            bool responseInteger = false;
            var disconnect = doCOMDisconnection();
            if (serial != null)
            {
                serial.Close();
                serial.Dispose();
            }
            if (disconnect == 0)
            {
                string portNumber = "COM" + comPort.ToString();
                serial = new SerialPort(portNumber, int.Parse(ComConstants.BAUDRATECOM), (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                                                   ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
                try
                {
                    if (serial.IsOpen)
                    {
                        serial.Close();
                    }
                    serial.Open();
                    responseInteger = true;
                    configdata = GetConfigData();
                    if (configdata != null)
                    {
                        configdata.commPortNumber = comPort;
                        this.configdata.tcpIp = configdata.tcpIp;
                        this.configdata.tcpPort = configdata.tcpPort;
                        configdata.isComHeartActive = comPort;
                        setConfiguration(configdata);
                        _connlistener.OnSuccess(PosLibConstant.COMCONNECTION_SUCCESS);
                    }
                }
                catch (IOException e)
                {
                    configdata.isAppidle = true;
                    setConfiguration(configdata);
                    _connlistener.OnFailure(PosLibConstant.COMFAIELD_ERROR);
                    Console.WriteLine("Problem connecting to host");
                    Console.WriteLine(e.ToString());
                    serial.Close();
                    serial.Dispose();
                }
                catch (UnauthorizedAccessException e)
                {
                    serial.Close();
                    serial.Dispose();
                    configdata.isAppidle = true;
                    setConfiguration(configdata);
                    _connlistener.OnFailure(PosLibConstant.COMFAIELD_ERROR);
                    Console.WriteLine("Unauthorized Access Exception" + e);
                }
            }
            return responseInteger;
        }
        public bool ComTransactionProcess(int comPort)
        {
            Log.Debug("enter comtransactionprocess()");
            bool responseInteger = false;
            var disconnect = doCOMDisconnection();
            if (serial != null)
            {
                serial.Close();
                serial.Dispose();
            }
            if (disconnect == 0)
            {
                int maxRetries = int.Parse(configdata.retrivalcount);
                int connectionTimeoutMillis = int.Parse(configdata.connectionTimeOut);
                string portNumber = "COM" + comPort.ToString();
                Log.Information("connected com port: " + portNumber);
                serial = new SerialPort(portNumber, int.Parse(ComConstants.BAUDRATECOM), (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                                                   ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
                for (int retry = 1; retry <= maxRetries; retry++)
                {
                    try
                    {
                        if (serial.IsOpen)
                        {
                            serial.Close();
                        }
                        serial.Open();
                        responseInteger = true;
                        break;
                    }
                    catch (IOException e)
                    {
                        if (retry < maxRetries)
                        {
                            Console.WriteLine($"Retry {retry} failed. Waiting for 30 seconds before retrying.");
                            Log.Information($"Retry {retry} failed. Waiting for 30 seconds before retrying.");
                            Thread.Sleep(connectionTimeoutMillis * 1000);
                        }
                        else
                        {
                            Console.WriteLine($"Max retries ({maxRetries}) reached. Unable to establish a connection.");
                            serial.Close();
                            serial.Dispose();
                        }
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        if (retry < maxRetries)
                        {
                            Console.WriteLine($"Retry {retry} failed. Waiting for 30 seconds before retrying.");
                            Thread.Sleep(connectionTimeoutMillis*1000);
                        }
                        else
                        {
                            Log.Information($"Max retries ({maxRetries}) reached. Unable to establish a connection.");
                            serial.Close();
                            serial.Dispose();
                        }
                        Console.WriteLine("Unauthorized Access Exception" + e);

                    }
                }
            }
            return responseInteger;
        }
        public Boolean CheckComStatus(int comPort,IConnectionListener icomlistener)
        {
            Log.Debug("Enter CheckComStatus method");
            _connlistener = icomlistener;
            bool responseInteger = false;
            var disconnect = doCOMDisconnection();
            if (serial != null)
            {
                serial.Close();
                serial.Dispose();
            }
            if (disconnect == 0)
            {
                string portNumber = "COM" + comPort.ToString();
                serial = new SerialPort(portNumber, int.Parse(ComConstants.BAUDRATECOM), (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                                                   ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
                try
                {
                    if (serial.IsOpen)
                    {
                        serial.Close();
                    }
                    serial.Open();
                    responseInteger = true;
                    configdata = GetConfigData();
                    if (configdata != null)
                    {
                        configdata.commPortNumber = comPort;
                        this.configdata.tcpIp = configdata.tcpIp;
                        this.configdata.tcpPort = configdata.tcpPort;
                        configdata.isComHeartActive = comPort;
                        setConfiguration(configdata);
                        _connlistener.OnSuccess(PosLibConstant.COMCONNECTION_SUCCESS);
                    }
                }
                catch (IOException e)
                {
                    _connlistener.OnFailure(PosLibConstant.COMFAIELD_ERROR);
                    Console.WriteLine("Problem connecting to host");
                    Console.WriteLine(e.ToString());
                    serial.Close();
                }
                catch (UnauthorizedAccessException e)
                {
                      serial.Close();
                      serial.Dispose();
                    _connlistener.OnFailure(PosLibConstant.COMFAIELD_ERROR);
                    Console.WriteLine("Unauthorized Access Exception" + e);
                }
                finally
                {
                    serial.Close();
                    serial.Dispose();
                }
            }
            return responseInteger;
        }
        public bool SendCOMTxnReq(string req)
        {
            if (serial.IsOpen)
            {
                serial.Close();
                serial.Dispose();
            }
            byte[] dataBytes = Encoding.UTF8.GetBytes(req);
            serial.Open();
            serial.WriteTimeout = 5000;
            serial.Write(dataBytes, 0, dataBytes.Length);
            return true;
        }
        public string ReceiveCOMTxnrep()
        {
            string responseString = string.Empty;
            try
            {
                byte[] buffer = new byte[6000];
                int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
                responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (serial.IsOpen)
                {
                    serial.Close();
                }
                serial.Close();
            }
            return responseString;
        }
        public void AddToDeviceList(string recivebuff)
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
                        deviceList.connectionMode = PosLibConstant.TCPIP;
                    }
                    else
                    {
                        deviceList.connectionMode = PosLibConstant.COM;
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
        public string GetComDeviceRequest()
        {
            ComDeviceRequest req = new ComDeviceRequest
            {
                cashierId = CommonConst.cashierID,
                msgType = CommonConst.msgType1_2,
                RFU1 = ""
            };
            string jsonrequest = JsonConvert.SerializeObject(req);
            Log.Information("Send UDP Request for COM :" + jsonrequest);
            return jsonrequest;
        }
        public ISet<string> getDeviceManagerComPort()
        {
            ISet<String> allComport = new HashSet<string>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(CommonConst.ManagementObjectSearcher);
            foreach (ManagementBaseObject obj in searcher.Get())
            {
                string caption = obj["Caption"].ToString();
                allComport.Add(caption);
            }
            return allComport;
        }
        public void scanOnlineDevice(IScanDeviceListener scanDeviceListener)
        {
            Log.Debug("Entering scanOnline Device mehtod");
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
                    cashierId = CommonConst.cashierID,
                    msgType = CommonConst.msgType1_1,
                    ecrIP = myIP,
                    ecrPort = CommonConst.ecrPort,
                    RFU1 = ""
                }; 
                string json = JsonConvert.SerializeObject(posData);
                Log.Information("Send UDP Request For TCP IP: " + json);
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
                    listener.onFailure(PosLibConstant.IO_EXC_MSG, PosLibConstant.IOEXCEPTION);
                    Log.Error("IOException" + e);
                }
            }
        }
        private void TcpListen()
        {
            Thread thread = new Thread(Run);
            thread.Priority = ThreadPriority.Normal;
            thread.Start();
        }
        public void Run()
        {

            TcpListener? server = null;
            bool isServerActive;
            string json = string.Empty;
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
                Timer timer = new Timer(StopServer, null, 15000, Timeout.Infinite);
                isServerActive = server.Server?.IsBound ?? false;
                while (isServerActive)
                {
                    TcpClient client = server.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    if (isServerActive)
                    {
                        byte[] buffer = new byte[5000];
                        stream.ReadTimeout = 5000;
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        json = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Log.Information("Accept UDP Response for TCP/IP" + json);
                    }
                    AddToDeviceList(json);
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
                server?.Stop();
                if (listener != null)
                {
                    if (deviceLists != null && deviceLists.Count > 0)
                    {
                        listener.onSuccess(deviceLists);
                    }
                    else
                    {
                        listener.onFailure("No Device Found Please Check NetWork", PosLibConstant.NO_DEV_FOUND);
                    }
                }
            }
            void StopServer(object state)
            {
                Log.Debug("Inside Stop Server Method");
                server?.Stop();
                Log.Information("Server stopped");
            }
        }
        public Boolean IsOnlineTest(string IP, int PORT, IConnectionListener onlinelistner)
        {
            _connlistener = onlinelistner;
            Log.Debug("Enter Isonline test method");
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
                    Log.Information("Connected ip:" + IP);
                    Log.Information("Connected port:" + PORT);
                    isConnected = true;
                    _connlistener.OnSuccess(PosLibConstant.TCPIPCONNECTION_SUCCESS);
                }
            }
            catch (SocketException e)
            {
                _connlistener.OnFailure(PosLibConstant.TCPIPFAIELD_ERROR);
                Console.WriteLine("Problem connecting to host");
                Console.WriteLine(e.ToString());
                isConnected = false;
                Log.Information("is tcp/ip connected:" + responseboolean);
                sock.Close();
            }
            if (configdata != null)
            {
                configdata.tcpIp = IP;
                configdata.tcpPort = PORT;
                configdata.connectionMode = PosLibConstant.TCPIP;
                this.configdata.commPortNumber = configdata.commPortNumber;
                setConfiguration(configdata);
            }
            return isConnected;
        }
        public Boolean ProcessOnlineTransaction(string IP, int PORT)
        {
            
            Log.Debug("Inside Isonline connection method");
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
                            bool success = result.AsyncWaitHandle.WaitOne(connectionTimeoutMillis * 1000);
                            if (success)
                            {
                                sock.EndConnect(result);
                                Log.Information("Connected ip:" + IP);
                                Log.Information("Connected port:" + PORT);
                                responseboolean = true;
                                break;
                            }
                        }
                        catch (SocketException e)
                        {
                            Log.Error("SocketException" + e);
                            if (retry < maxRetries)
                            {
                                try
                                {
                                    Log.Information($"Retry {retry} failed. Waiting for 30 seconds before retrying.");
                                    Thread.Sleep(1000);
                                }
                                catch (ThreadInterruptedException ie)
                                {
                                    Log.Error("ThreadInterruptedException" + ie);
                                    Thread.CurrentThread.Interrupt();
                                }
                            }
                            else
                            {
                                Log.Information($"Max retries ({maxRetries}) reached. Unable to establish a connection.");
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
                sock.Close();
            }
            if (configdata != null)
            {
                configdata.tcpIp = IP;
                configdata.tcpPort = PORT;
                configdata.connectionMode = PosLibConstant.TCPIP;
                this.configdata.retrivalcount = configdata.retrivalcount;
                this.configdata.connectionTimeOut = configdata.connectionTimeOut;
                this.configdata.commPortNumber = configdata.commPortNumber;
                setConfiguration(configdata);
            }
            return responseboolean;
        }
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
        public string ReceiveTcpIpTxnData()
        {
            configdata.isAppidle = false;
            string value = CheckTcpIpHeartBeat();
            byte[] responseData = new byte[6000];
            Thread.Sleep(1000);
            int bytesReceived = sock.Receive(responseData);
            string responseString = Encoding.ASCII.GetString(responseData, 0, bytesReceived);
            Console.WriteLine("Transaction Response:" + responseString);
            sock.Close();
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