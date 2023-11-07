
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
using System.Diagnostics;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Xceed.Wpf.Toolkit;

namespace PosLibs.ECRLibrary.Service
{
    public class ConnectionService
    {
       
        public ConnectionService() {
           
        }
        private  Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private  SerialPort serial = new SerialPort();
        private string PortCom = string.Empty;
        private IScanDeviceListener? listener;
        private ComEventListener? eventListener;
        readonly List<DeviceList> deviceLists = new List<DeviceList>();
        private string fullcomportName = string.Empty;
        ConfigData configdata = new ConfigData();
        private bool isConnected = false;
        private const string PortSettingsFilePath = PosLibConstant.FILE_PATH;
        private string ReadSettingsFilePath = ComConstants.createlogfile;
        public int setConfiguration(ConfigData configData)
        {
            Log.Debug("entering setConfiguration()");
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
                retry = configdata.retry,
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
                isAppidle = configData.isAppidle,
                logtype=configdata.logtype,
                retainDay=configData.retainDay,
                isDeviceNumberMatch=configData.isDeviceNumberMatch,
            };
            if (!Directory.Exists(PortSettingsFilePath))
            {
                Directory.CreateDirectory(PortSettingsFilePath);
            }
            string fileName = PosLibConstant.FILE_NAME;
            string filePath = Path.Combine(PortSettingsFilePath, fileName);
            ReadSettingsFilePath = filePath;
            string json = JsonConvert.SerializeObject(configData);
            Log.Debug("set configuration data: " + json);
            try
            {
                File.WriteAllText(filePath, json);
            }
            catch(FileLoadException ex)
            {
               
                Log.Error("FileLoad Exception");
                Console.WriteLine(ex.Message);
                return -1;
            }
            catch (Exception ex)
            {

                Log.Error("FileLoad Exception");
                Console.WriteLine(ex.Message);
                return -1;
            }
            return 0;
        }
        public int getConfiguration(out ConfigData configData)
        {
            Log.Debug("entering GetConfigData()");

            configData = new ConfigData(); // Create a new instance of ConfigData

            if (File.Exists(ReadSettingsFilePath))
            {
                string json = File.ReadAllText(ReadSettingsFilePath);
                Log.Debug("get configData: "+json);
                configData = JsonConvert.DeserializeObject<ConfigData>(json)!;

                if (configData != null)
                {
                    return 0;
                }
            }
            return -1;
        }
        public void checkTcpComStatus(ComEventListener comEventListener)
        {
            Log.Information("Entering checkTcpComStatus() method");
            eventListener = comEventListener;
            if (string.IsNullOrWhiteSpace(configdata.tcpIpPort) || string.IsNullOrWhiteSpace(configdata.tcpIp))
            {
                 eventListener.OnFailure(PosLibConstant.TCPIPHEALTHNULLVALUE);
                Log.Information("TCPIP IP and PORT value are null : " + PosLibConstant.TCPIPHEALTHNULLVALUE);
                return;
            }

            if (!testTCP(configdata.tcpIp, configdata.tcpPort))
            {
                Log.Information("in comEventListener on onEvent:" + PosLibConstant.TCPIPHEALTHINACTIVE);
                Log.Information("in comEventListener TCPIP  : " + "failed");
                eventListener.OnFailure(PosLibConstant.TCPIPHEALTHINACTIVE); 
                return;
            }
            if (string.IsNullOrWhiteSpace(configdata.tcpIp) || !configdata.isAppidle || string.IsNullOrWhiteSpace(configdata.tcpIpPort))
            {
                eventListener.OnFailure(PosLibConstant.TCPIPHEALTHINACTIVE);
            }
            Log.Information("in comEventListener on onEvent:" + PosLibConstant.TCPIPHEALTHACTIVE);
            Log.Information("in comEventListener TCPIP  : " + "Success");
            CheckPaymentHealthCheckRequest healthCheckRequest = new CheckPaymentHealthCheckRequest();
            string requestBody = healthCheckRequest.CheckTcpIpHealthRequest();
            bool isTerminalReachable = IsHostReachable(configdata.tcpIpaddress, int.Parse(configdata.tcpIpPort), requestBody);
            if (isTerminalReachable)
            {
                eventListener.OnSuccess(PosLibConstant.TCPIPHEALTHACTIVE);
            }
            else
            {
                eventListener.OnFailure(PosLibConstant.TCPIPHEALTHINACTIVE);
            }
        }
        private bool IsHostReachable(string IP, int port,string requestBody)
        {
            getConfiguration(out configdata);
            if (configdata.isAppidle) { 
            IPAddress host = IPAddress.Parse(IP);
            IPEndPoint hostEndPoint = new IPEndPoint(host, port);
                using (Socket sockt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {

                    if (requestBody != null)
                    {
                        try
                        {
                            sockt.Connect(hostEndPoint);
                            Log.Information("Send TCP/IP healthCheckup request: " + requestBody);
                            byte[] requestData = Encoding.ASCII.GetBytes(requestBody);
                            sockt.SendTimeout = PosLibConstant.SENDTIMEOUT;
                            sockt.Send(requestData);
                            byte[] responseData = new byte[6000];
                            sockt.ReceiveTimeout = 3000;
                            int bytesReceived = sockt.Receive(responseData);
                            string responseString = Encoding.ASCII.GetString(responseData, 0, bytesReceived);
                            PaymentHealthResponse? paymentHealthResponse = JsonConvert.DeserializeObject<PaymentHealthResponse>(responseString);
                            Log.Information("Receive TCP/IP healthCheckup response: " + responseString);
                            Log.Information("Exiting checkTcpComStatus() method");
                            sockt.Close();
                            if (paymentHealthResponse != null)
                            {
                                return paymentHealthResponse.isPayAppActive;
                            }
                        }
                        catch (SocketException e)
                        {
                            Console.WriteLine("Socket is closed please try again" + e);
                            Log.Information("TCPIP disconnected : " + "1001");
                            Log.Information("Exiting checkTcpComStatus() method");
                            return false;
                        }
                    }
                }  
            }
            return false;
        }
        public void checkComStatus(ComEventListener comEventListener)
        {
            eventListener = comEventListener;
            Log.Debug("Entering checkComHeartBeat() method");
            getConfiguration(out configdata);
            if (configdata.isAppidle)
            { string terminalserialNumber = configdata.comserialNumber;
                if (testSerialCom(configdata.commPortNumber))
                { 
                    Log.Information("in comEventListener on onEvent : " + PosLibConstant.COMHEALTHACTIVE);
                    Log.Information("in comEventListener COM : Connected ");
                    CheckPaymentHealthCheckRequest comhealthcheckrequest = new CheckPaymentHealthCheckRequest();
                    string requestbody = comhealthcheckrequest.CheckCompHealthRequest();
                    bool status = CheckBothConnection(configdata.commPortNumber, requestbody);
                    if (status)
                    {
                         eventListener.OnSuccess(PosLibConstant.COMHEALTHACTIVE);
                    }
                    else
                    {
                        getConfiguration(out configdata);
                        configdata.isDeviceNumberMatch = false;
                        setConfiguration(configdata);
                        eventListener.OnFailure(PosLibConstant.COMHEALTHINACTIVE);
                    }
                }
                else
                {
                    Log.Information("in comEventListener on onEvent : " + PosLibConstant.COMHEALTHINACTIVE);
                    Log.Information("in comEventListener COM : DisConnected ");
                    eventListener.OnFailure(PosLibConstant.COMHEALTHINACTIVE);
                }
            }
            else
            {
                eventListener.OnFailure (PosLibConstant.COMHEALTHINACTIVE);
            }
        }
        public bool CheckBothConnection(int comportnumber, string comrequestbody)
        {
            if (!configdata.isAppidle)
            {
                return false;
            }
            string deviceserialNumber = string.Empty;
            string portcom = "COM" + comportnumber;
            serial = new SerialPort(portcom, int.Parse(ComConstants.BAUDRATECOM),
                  (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                  ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
            
                try
                {      
                serial.Open();
                serial.WriteTimeout = 3000;
                byte[] dataBytes = Encoding.UTF8.GetBytes(comrequestbody);
                serial.Write(dataBytes, 0, dataBytes.Length);
                Log.Information("Send COM healthCheckup request: " + comrequestbody);
                byte[] buffer = new byte[6000];
                serial.ReadTimeout = 5000;
                int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
                string responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                PaymentHealthResponse paymentHealthResponse = JsonConvert.DeserializeObject<PaymentHealthResponse>(responseString)!;
                 deviceserialNumber = paymentHealthResponse.slNo;
                Log.Information("device serialNumber:" + deviceserialNumber);
                Log.Information("Receive comhealthCheck response:" + responseString);
                Log.Information("Exiting CheckCOMHeartBeat() method");
                configdata.deviceHealthCheckSerialNumber = configdata.comserialNumber;
                serial.Close();
                if (configdata.deviceHealthCheckSerialNumber == deviceserialNumber)
                {
                    getConfiguration(out configdata);
                    configdata.isDeviceNumberMatch = true;
                    setConfiguration(configdata);
                    return paymentHealthResponse?.isPayAppActive ?? false;
                }
                return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("socket error: " + ex);
                    Log.Information("Exiting CheckCOMHeartBeat() method");
                    serial.Close();
                    serial = null!;
                    Thread.Sleep(3000);
                    return false;
                }
        }
        public bool checkComConn()
        {
            if (serial != null)
            {
                return serial.IsOpen;
            }
            return false;

        }
        public string checkComport(string port)
        {
            Log.Debug("enter chechComport()");
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
        public void scanSerialDevice(IScanDeviceListener scanDeviceListener)
        {
            Log.Debug("Enter scanSerialDevice method");
            Log.Information("Entering ScanSerialDevice Method");
            this.listener = scanDeviceListener;
            ISet<string> allUSBcomport = getDeviceManagerComPort();
            ISet<string> validComports = FilterValidComports(allUSBcomport);

            if (validComports.Count == 0)
            {
                LogInformationAndExit("failed to send the com discovery request due to valid com port is not available");
                return;
            }

            string[] comPorts = GetComPorts(validComports);

            foreach (string comPort in comPorts)
            {
                try
                {
                    string portcom = "COM" + comPort;
                    InitializeAndOpenSerialPort(portcom);
                    if (SendSerialDiscoveryData(GetComDeviceRequest()))
                    {
                        string responseString = SerialrevData();
                        Log.Information("receive com discovery response:" + responseString);
                        fullcomportName = checkComport(portcom);
                        PortCom = fullcomportName;

                        if (!string.IsNullOrEmpty(responseString))
                        {
                            AddToDeviceList(responseString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
                finally
                {
                    CloseSerialPort();
                    Thread.Sleep(1000);
                }
            }

            if (deviceLists != null && deviceLists.Count > 0)
            {
                listener.onSuccess(deviceLists);
                Log.Information("scan serial discovery done");
            }

            Log.Information("Exit ScanSerialDevice method");
        }
        private void LogInformationAndExit(string message)
        {
            Log.Information(message);
            Log.Information("Exiting ScanSerialDevice Method");
        }
        private void InitializeAndOpenSerialPort(string portcom)
        {
            InitializeSerialPort(portcom);
            serial.Open();
        }
        private void HandleException(Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Log.Error(" COM serial not connected  Exception");
            CloseSerialPort();
        }
        private void CloseSerialPort()
        {
            if (serial.IsOpen)
            {
                serial.Close();
            }
        }
        private ISet<string> FilterValidComports(ISet<string> allUSBcomport)
        {
            HashSet<string> domainWords = new HashSet<string> { "Daemon", };
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
            Log.Debug("Entring Initialize Serial port method");
            serial = new SerialPort(portcom, int.Parse(ComConstants.BAUDRATECOM), (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                                                  ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
            Log.Information("serial port Open");
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
            Log.Information("send com discovery req successfully");
            return true;
        }
        public string SerialrevData()
        {
            configdata.isAppidle = false;
            byte[] buffer = new byte[5000];
            serial.ReadTimeout = 3000;
            int bytesRead = serial.BaseStream.Read(buffer, 0, buffer.Length);
            string responseString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Log.Information("serial close successfully");
            serial.Close();
            return responseString;
        }
        public bool ComTransactionProcess(int comPort)
        {
            bool responseInteger = false;
            getConfiguration(out configdata);
            if (configdata.isDeviceNumberMatch)
            {
            Log.Debug("enter comtransactionprocess()");
            
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
                    Log.Information("Connected COM :" + portNumber);
                }
                catch (IOException e)
                {
                    Console.WriteLine("IOException Exception" + e);
                    serial.Close();
                    serial.Dispose();
                }
                catch (UnauthorizedAccessException e)
                {
                    serial.Close();
                    serial.Dispose();
                    Console.WriteLine("Unauthorized Access Exception" + e);
                }
            }
            }
            serial.Close();
            return responseInteger;
        }
        public Boolean testSerialCom(int comPort)
        {
            Log.Debug("enter testSerialCom method");
            bool responseInteger = false;
            var disconnect = doCOMDisconnection();
            if (serial != null)
            {
                serial.Close();
                serial = null!;
            }
            if (disconnect == 0)
            {
                string portNumber = "COM" + comPort.ToString();
                serial = new SerialPort(portNumber, int.Parse(ComConstants.BAUDRATECOM), (Parity)Enum.Parse(typeof(Parity), ComConstants.PARITYCOM),
                                                   ComConstants.DATABITSCOM, (StopBits)Enum.Parse(typeof(StopBits), ComConstants.STOPBITSCOM.ToString()));
                try
                {

                    serial.Open();
                    responseInteger = true;
                    serial.Close();
                    int result = getConfiguration(out configdata);
                    if (result==0)
                    {
                        configdata.commPortNumber = comPort;
                        this.configdata.tcpIp = configdata.tcpIp;
                        this.configdata.tcpPort = configdata.tcpPort;
                        setConfiguration(configdata);
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("Problem connecting to host");
                    Log.Information(PosLibConstant.COMFAIELD_ERROR, PosLibConstant.COMERRORCODEFAIL);
                    Console.WriteLine(e.ToString());
                    serial.Close();
                }
                catch (UnauthorizedAccessException e)
                {
                      serial.Close();
                      serial.Dispose();
                    Log.Information(PosLibConstant.COMFAIELD_ERROR, PosLibConstant.COMERRORCODEFAIL);
                    Console.WriteLine("Unauthorized Access Exception" + e);
                }
            }
            return responseInteger;
        }
        public bool SendCOMTxnReq(string req)
        {
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
                
                byte[] responseData = new byte[6000];
                serial.ReadTimeout = int.Parse(configdata.connectionTimeOut)*1000;
                int bytesReceived = serial.Read(responseData, 0, responseData.Length);
                if (bytesReceived > 0)
                {
                    serial.Close();
                    return responseString = Encoding.ASCII.GetString(responseData, 0, bytesReceived);
                }
                else
                {
                    serial.Close();
                    return responseString = "received failed: 2204";
                   
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.ToString());
                serial.Close();
                serial = null!;
                Thread.Sleep(25000);
                ComTransactionProcess(configdata.commPortNumber);
            }
            catch(IOException e)
            {
                Console.WriteLine(e.ToString());
                serial.Close();
                serial = null!;
                Thread.Sleep(25000);
                ComTransactionProcess(configdata.commPortNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                serial.Close();
                serial = null!;
                Thread.Sleep(25000);
                ComTransactionProcess(configdata.commPortNumber);
            }
            return responseString;
        }
        public void AddToDeviceList(string recivebuff)
        {
            try
            {
                DeviceList deviceList = new DeviceList();

                TerminalConnectivityResponse terminalConnectivity = JsonConvert.DeserializeObject<TerminalConnectivityResponse>(recivebuff)!;
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
            Log.Information("Entering getComDeviceRequest method");
            ComDeviceRequest req = new ComDeviceRequest
            {
                cashierId = configdata.CashierID,
                msgType = CommonConst.msgType1_2,
                RFU1 = ""
            };
            string jsonrequest = JsonConvert.SerializeObject(req);
            Log.Information("Request packet data sent: :" + jsonrequest);
            return jsonrequest;
        }
        public ISet<string> getDeviceManagerComPort()
        {
            ISet<string> allComport = new HashSet<string>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(CommonConst.ManagementObjectSearcher);
            foreach (ManagementBaseObject obj in searcher.Get())
            {
                string caption = obj["Caption"].ToString();
                allComport.Add(caption);
            }
            return allComport;
        }
        public void scanOnlinePOSDevice(IScanDeviceListener scanDeviceListener)
        {
            Log.Debug("Entering scanOnline Device mehtod");
            Log.Information("Entering scanOnline Device Method");
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
                Log.Information("System IP address:" + myIP);

                EcrTcpipRequest posData = new EcrTcpipRequest
                {
                    cashierId = CommonConst.cashierID,
                    msgType = CommonConst.msgType1_1,
                    ecrIP = myIP,
                    ecrPort = CommonConst.ecrPort,
                    RFU1 = ""
                };
               
                string json = JsonConvert.SerializeObject(posData);
                Log.Information("Request packet data sent: " + json);
                var message = Encoding.ASCII.GetBytes(json);
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                while (stopwatch.Elapsed.TotalSeconds < 30) // Run for a maximum of 30 seconds
                {
                    client.Send(message, message.Length, broadcastEndpoint);
                    // Sleep for 1 second

                }
                
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
                    Console.WriteLine(ExceptionConst.IOEXCEPTION + e);
                    Log.Error(ExceptionConst.IOEXCEPTION);
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
                Console.WriteLine("Socket Exception" + ex);
                Log.Error("Socket ExceptionSystem.Net.Sockets.SocketException");
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
                        Log.Information("Exiting ScanOnlinePosDevice method");
                    }
                    else
                    {
                        listener.onFailure("No Device Found Please Check NetWork", PosLibConstant.NO_DEV_FOUND);
                    }
                }

            }
            void StopServer(object state)
            {
                server?.Stop();
                Log.Information("Server stopped");
            }
        }
        public Boolean testTCP(string IP, int PORT)
        {
            using (Socket sockt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Log.Debug("Enter Isonline test method");
                IPAddress host = IPAddress.Parse(IP);
                IPEndPoint hostep = new IPEndPoint(host, PORT);
                try
                {
                    int resdisconnect = doTCPIPDisconnection();
                    if (resdisconnect == 0)
                    {
                        sockt.Connect(hostep);
                        isConnected = true;
                        if (configdata != null)
                        {
                            configdata.tcpIp = IP;
                            configdata.tcpPort = PORT;
                            this.configdata.commPortNumber = configdata.commPortNumber;
                            setConfiguration(configdata);
                        }
                        sock.Close();
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Problem connecting to host");
                    Console.WriteLine(e.ToString());
                    isConnected = false;
                    sock.Close();
                }
               
            }
            return isConnected;
        }
        public bool ProcessOnlineConnection(string ipAddress, int port)
        {
            
            getConfiguration(out configdata);
            if (doTCPIPDisconnection() != 0)
            {
                return false;
            }

            IPAddress host = IPAddress.Parse(ipAddress);
            IPEndPoint hostEndPoint = new IPEndPoint(host, port);
            int maxRetries = int.Parse(configdata.retry);
            int userinputconnectionTimeoutMills = int.Parse(configdata.connectionTimeOut);
            int connectionTimeoutMillis = userinputconnectionTimeoutMills * 1000;

            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (maxRetries == 0)
            {
                maxRetries = 1;
            }

            for (int retry = 1; retry <=maxRetries; retry++)
            {
                DateTime startTime = DateTime.Now;

                try
                {
                    if (ConnectWithTimeout(sock, hostEndPoint, connectionTimeoutMillis))
                    {
                        return true;
                    }
                }
                catch (SocketException e)
                {
                    HandleSocketException(e);
                    DateTime endTime = DateTime.Now;
                    int elapsedMillis = (int)(endTime - startTime).TotalMilliseconds;
                    int remainingTimeMillis = connectionTimeoutMillis - elapsedMillis;

                    if (elapsedMillis < 20000)
                    {
                        break;
                    }
                    else
                    {
                        HandleRetry(maxRetries, retry, userinputconnectionTimeoutMills, remainingTimeMillis);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    DateTime endTime = DateTime.Now;
                    int elapsedMillis = (int)(endTime - startTime).TotalMilliseconds;
                    int remainingTimeMillis = connectionTimeoutMillis - elapsedMillis;

                    if (elapsedMillis < 20000)
                    {
                        break;
                    }
                    else
                    {
                        HandleRetry(maxRetries, retry, userinputconnectionTimeoutMills, remainingTimeMillis);
                    }
                }
            }

            return false;
        }
        private bool ConnectWithTimeout(Socket socket, IPEndPoint endPoint, int timeoutMillis)
        {
            IAsyncResult result = socket.BeginConnect(endPoint, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeoutMillis);

            if (success)
            {
                socket.EndConnect(result);
                return true;
            }

            return false;
        }
        private void HandleSocketException(SocketException e)
        {
            Console.WriteLine("Socket Exception " + e);
            Log.Error(ExceptionConst.SOCKETEXCEPTION);
        }
        private void HandleRetry(int maxRetries, int retry, int userinputconnectionTimeoutMills, int remainingTimeMillis)
        {
            if (remainingTimeMillis > 0)
            {
                Thread.Sleep(remainingTimeMillis);
            }

            if (retry <= maxRetries)
            {
                try
                {
                    Log.Information($"Retry {retry} failed. Waiting for {userinputconnectionTimeoutMills} seconds before retrying.");
                    Thread.Sleep(1000);
                }
                catch (ThreadInterruptedException ie)
                {
                    Log.Error(ExceptionConst.THREADEXC + ie);
                    Thread.CurrentThread.Interrupt();
                }
            }
        }
        public bool SendTcpIpTxnData(string requestdata)
        {
            bool responseboolen = false;
            if (requestdata != null)
            {
                try
                {
                    byte[] requestData = Encoding.ASCII.GetBytes(requestdata);
                    sock.SendTimeout = PosLibConstant.SENDTIMEOUT;
                    sock.Send(requestData);
                    responseboolen = true;
                    Log.Information("tcp/ip transaction data send successfully");
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Socket is closed please try agian"+e);
                    Log.Error("send tcp/Ip txn request failed");
                    responseboolen = false;
                }
            }
            return responseboolen;
        }
        public string ReceiveTcpIpTxnData()
        {
            try
            {
                Log.Information("Enter Receive tcp/ip txn response");
                byte[] responseData = new byte[6000];
                sock.ReceiveTimeout = PosLibConstant.REVTIMEOUT;
                int bytesReceived = sock.Receive(responseData);
                Log.Information("waiting tcp/ip response receive timeout" + PosLibConstant.REVTIMEOUT);
                string responseString = Encoding.ASCII.GetString(responseData, 0, bytesReceived);
                Console.WriteLine("Transaction Response:" + responseString);
                sock.Close();
                return responseString;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Socket Exception " + e);
                Log.Error("Socket Exception");
                sock.Close();
                Thread.Sleep(10000);
                return "";
            }
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