using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Newtonsoft.Json.Linq;
using PosLibs.ECRLibrary.Common;
using PosLibs.ECRLibrary.Logger;
using PosLibs.ECRLibrary.Model;
using PosLibs.ECRLibrary.Service;
using Serilog.Core;
using Path = System.IO.Path;
using Microsoft.WindowsAPICodePack.Dialogs;
using PosLibs.ECRLibrary.Common.Interface;
using Newtonsoft.Json;
using POSLIB.Model;
using System.Windows.Media;
using Serilog;
using System.Diagnostics;
using System.Timers;
using Timer = System.Threading.Timer;

namespace POSLIB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {

        // private Properties.Settings settings = Properties.Settings.Default;


        string TerminalId = string.Empty;
        int status = 0;
        string cashRegister = string.Empty;
        string transaction = string.Empty;

        string prvsRCRNumber;
        static string textFocus = string.Empty;
        static string hostIp = string.Empty;
        static string port;
        static string PortCom;
        static string IPortCom;
        static string baudRateCom = string.Empty;
        static string parityCom = string.Empty;
        static int dataBitsCom;
        static int stopBitsCom;
        static int result = 1;
        static bool isOnlineDevice;
        static int resultDisconnect;
        static string tcpComSelect = string.Empty;
        static string inputReqData = "";
        static string transactionType = "";
        static string etpInput = "";
        static string ecrReferenceNo = "";
        static string szSignature = "";
        string originalTransactionDate = "";
        static string originalTransactionDateRefund = "";
        static string name;
        static bool printCheck;
        static bool ECRrefNum;
        static bool pfcCheck;
        string savetcpIp = string.Empty;
        string savetcpport = string.Empty;
        string savedeviceid = string.Empty;
        string savecomdeviceid = string.Empty;
        string savetcpserialno = string.Empty;
        string savecomserialno = string.Empty;
        string savecomport = string.Empty;
        static string transTypeSelected;
        static string transTypeSelectedPos;
        static string AMOUNT;
        static string ECRREFtext;
        static string CASHBACK;
        static string RRN;
        static string OAC;
        static string CashRegisterNumber;
        static string VendorID;
        static string VendorTerminaltype;
        static string TRSMID;
        static string VendorKeyIndex;
        static string SAMAKeyIndex;
        static string BillerID;
        static string BillNumber;
        static string prvsECRNumber;
        static string BUFFERSENDtext;
        static string BUFFERRECEIVEDtext;
        static string msgShow;
        static string ECRREferenceNumber;
        static string htmlString = "";
        static string AutohostIp = string.Empty;
        static string Autoport = string.Empty;
        string sendData;
        string recount = string.Empty;
        string connectionTimeout = string.Empty;
        static int countAmount = 0;
        static int countCB = 0;
        public string tcpdeviceID = string.Empty;
        public string comdeviceID = string.Empty;



        static string deviceName = string.Empty;

        BackgroundWorker worker;
        BackgroundWorker workerSend;

        private ObservableCollection<DeviceList> data = new ObservableCollection<DeviceList>();
        public ObservableCollection<DeviceList> comdata
        {
            get { return data; }
            set { data = value; OnPropertyChanged(); }
        }


        ConfigData? fetchData;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            InitiateTimer();
            DataContext = this;
            GetCOMPort();
            TerminalConnectionCheckerW();
            loadUserSettings();
            createLogFile();
            showData();
            Log.Information("-:Application Start:-");
        }

        private void InitiateTimer()
        {
            System.Timers.Timer newTimer = new System.Timers.Timer();
            newTimer.Elapsed += new ElapsedEventHandler(deleteFileifNeeded);
            newTimer.Interval = 5000;
            newTimer.Start();
        }
        
        private void deleteFileifNeeded(object source, ElapsedEventArgs e)
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                filepath = Filepath.Text;
            });

            string expirationDate = Properties.Settings.Default.retentionDate;

            if (string.IsNullOrEmpty(expirationDate))
            {
                expirationDate = DateTime.Now.AddDays(double.Parse(NoDay.Text) - 1).ToString();
                Properties.Settings.Default.retentionDate = expirationDate;
            }

            if (LogFile.deleteFileifNeeded(filepath, expirationDate))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Properties.Settings.Default.retentionDate = DateTime.Now.AddDays(double.Parse(NoDay.Text) - 1).ToString();
                    createLogFile();
                });
            }
            Properties.Settings.Default.Save();
        }

        private void loadUserSettings()
        {

            if (Properties.Settings.Default.ConnectionFallback == "True")
            {
                connectivityFallbackCheckBox.IsChecked = true;
            }
            else
            {
                connectivityFallbackCheckBox.IsChecked = false;
            }

            if (Properties.Settings.Default.Priority != "")
            {
                comboBox.Text = Properties.Settings.Default.Priority;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.ConnectionTimeOut))
            {
                ConnectionTimeOut.Text = "30";
            }
            else
            {
                ConnectionTimeOut.Text = Properties.Settings.Default.ConnectionTimeOut;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.RetryTime))
            {
                retrivalCount.Text = "3";
            }
            else
            {
                retrivalCount.Text = Properties.Settings.Default.RetryTime;
            }

            if (Properties.Settings.Default.LogOption == "True")
            {
                isEnabledlog.IsChecked = true;
                islogAllowed = true;
            }
            else
            {
                isEnabledlog.IsChecked = false;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.Path))
            {
                Filepath.Text = ComConstants.logFilepath;
            }
            else
            {
                Filepath.Text = Properties.Settings.Default.Path;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.RetainDays))
            {
                NoDay.Text = "2";
            }
            else
            {
                NoDay.Text = Properties.Settings.Default.RetainDays;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.LogLevel))
            {
                LogLevel.Text = "error";
            }
            else
            {
                LogLevel.Text = Properties.Settings.Default.LogLevel;
            }
        }
        public void createLogFile()
        {
            string loglevelval = Properties.Settings.Default.LogLevel;
            int noOfDayValue = int.Parse(NoDay.Text);
            switch (loglevelval.ToLower()) // Convert input to lowercase for case-insensitivity
            {
                case "error":
                    loglevel = "1";
                    break;
                case "debug":
                    loglevel = "4";
                    break;
                case "information":
                    loglevel = "3";
                    break;
                default:
                    loglevel = "1";
                    break;
            }

            filepath = Filepath.Text;

            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
            if (islogAllowed)
            {
                LogFile.SetLogOptions(int.Parse(loglevel), true, filepath, noOfDayValue);
            }
        }

        public void showData()
        {
            ConfigData fetchData = new ConfigData();
            int result = obj.getConfiguration(out fetchData);
            if (fetchData.isAppidle == false)
            {
                fetchData.isAppidle = true;
                obj.setConfiguration(fetchData);
            }

            tcpip.Text = fetchData.tcpIpaddress;
            tcpport.Text = fetchData.tcpIpPort;
            serialNo.Text = fetchData.tcpIpSerialNumber;
            comfullname.Text = fetchData.comfullName;
            comserialNo.Text = fetchData.comserialNumber;
            TCPIPIP.Text = fetchData.tcpIpPort;
            Filepath.Text = Filepath.Text;
            TCPPORT.Text = fetchData.tcpIpaddress;
            TCPSerialNO.Text = fetchData.tcpIpSerialNumber;
            CashierID.Text = fetchData.CashierID;
            CashireName.Text = fetchData.CashierName;

            for (int i = 0; i < fetchData?.communicationPriorityList?.Length; i++)
            {
                if (fetchData.communicationPriorityList[i] == "TCP/IP")
                {
                    TCPlbl.Text = "TCP IP";
                    COMlbl.Text = "COM";
                    TCPIPGrid.Visibility = Visibility.Visible;
                    TCPIPGrid1.Visibility = Visibility.Hidden;
                    COMGrid.Visibility = Visibility.Visible;
                    COMGrid1.Visibility = Visibility.Hidden;
                    TCPIPIP1.Text = fetchData.tcpIpPort;
                    TCPSrialNO1.Text = fetchData.tcpIpSerialNumber;
                    TCPIPPORT.Text = fetchData.tcpIpaddress;
                    TCPIPDeviceID.Text = fetchData.tcpIpDeviceId;
                    COMDeviceID.Text = fetchData.comDeviceId;
                    COMSerialPort.Text = fetchData.comfullName;
                    COMSrialNO.Text = fetchData.comserialNumber;
                    break;

                }
                else
                {
                    TCPlbl1.Text = "COM";
                    COMlbl1.Text = "TCP IP";
                    TCPIPGrid.Visibility = Visibility.Hidden;
                    TCPIPGrid1.Visibility = Visibility.Visible;
                    COMGrid.Visibility = Visibility.Hidden;
                    COMGrid1.Visibility = Visibility.Visible;
                    COMDeviceID1.Text = fetchData.comserialNumber;
                    COMSerialPort1.Text = fetchData.comDeviceId;
                    COMSerialNO.Text = fetchData.comfullName;
                    TCPIPIP1.Text = fetchData.tcpIpaddress;
                    TCPSrialNO1.Text = fetchData.tcpIpSerialNumber;
                    TCPIPPORT.Text = fetchData.tcpIpPort;
                    TCPDeviceID1.Text = fetchData.tcpIpDeviceId;
                    break;
                }
            }
        }

        public static List<DeviceList> serialdevice = new List<DeviceList>();
        public List<DeviceList> serialdevicelist = new List<DeviceList>();
        public class ScanLisnter : IScanDeviceListener
        {
            public void onFailure(string errorMsg, int errorCode)
            {
                Console.WriteLine("Error Message");
            }

            public void onSuccess(List<DeviceList> list)
            {
                if (list != null)
                {
                    serialdevice = list;
                }
            }
        }
        private void DOCOMConnection()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += work_Comdevices_scan;

            worker.ProgressChanged += worker_ProgressChanged;

            worker.RunWorkerAsync();

        }

        private void GetCOMPort()
        {
            string[] ports = SerialPort.GetPortNames();
            try
            {

                string query = "SELECT * FROM Win32_SerialPort";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                foreach (ManagementObject device in searcher.Get())
                {
                    deviceName = (string)device["Name"];
                    PORTCOM.Items.Add(deviceName);
                }
            }
            catch (ManagementException ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
        private void TCPCOMOnChange(object sender, RoutedEventArgs e)
        {

            if (rdbCom.IsChecked == true)
            {
                tcpComSelect = "TCP/IP";
                grpTcpSetting.Visibility = Visibility.Hidden;
                grpComSetting.Visibility = Visibility.Visible;
            }
            if (rdbTcp.IsChecked == true)
            {
                tcpComSelect = "COM";

                grpComSetting.Visibility = Visibility.Hidden;
                grpTcpSetting.Visibility = Visibility.Visible;
                grpTcpSetting.IsEnabled = true;
            }

            if (tcpComSelect.ToString() == "TCP/IP")
            {
                PORTCOM.IsEnabled = true;
                txtIpAddress.IsEnabled = false;
                txtPort.IsEnabled = false;
                BAUDSPEED.IsEnabled = true;
                PARITY.IsEnabled = true;
                DATABITS.IsEnabled = true;
                STOPBITS.IsEnabled = true;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            else
            {
                txtPort.IsEnabled = true;
                txtIpAddress.IsEnabled = true;
                BAUDSPEED.IsEnabled = false;
                PARITY.IsEnabled = false;
                DATABITS.IsEnabled = false;
                STOPBITS.IsEnabled = false;
                // txtPort.Text = "";
                BAUDSPEED.SelectedIndex = 5;
                PARITY.SelectedIndex = 0;
                DATABITS.SelectedIndex = 3;
                STOPBITS.SelectedIndex = 0;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
        }
        static bool statusSerialConn = false;
        DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        public Timer connectionCheckTimer;
        private static DateTime lastActivityTime = DateTime.Now;
        private static DispatcherTimer idleTimer;
        private const int IdleThresholdMilliseconds = 15000;

        public void TerminalConnectionCheckerW()
        {
                connectionCheckTimer = new Timer(ConnectionCheckTimer_Tick, null, TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(30));
        }
        private void ConnectionCheckTimer_Tick(object state)
        {
            isAppIdle();
            Thread.Sleep(10000);
        }
        public void isAppIdle()
        {
            ConfigData value = new ConfigData();
            int  result = obj.getConfiguration(out value);
            if (value != null)
            {
                if (value.isAppidle)
                { 
                     checkComHeatBeat();
                     checkTcpIpHeatBeat(); 
                }
            }
        }
        public static int statusCode = 0;
        public class TransactionDriveHeart : ComEventListener
        {
            public void OnFailure(string errorMsg)
            {
                statusCode = int.Parse(errorMsg);
            }
            public void OnSuccess(string paymentResponse)
            {
                statusCode = int.Parse(paymentResponse);
            }
        }
        public void checkComHeatBeat()
        {
            TransactionDriveHeart tcplistner = new TransactionDriveHeart();
            obj.checkComStatus(tcplistner);
            switch (statusCode)
            {
                case 2000:
                    Dispatcher.Invoke(() => COMlabel.Content = "ACTIVE");
                    Dispatcher.Invoke(() => COMlabel1.Content = "ACTIVE");
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0x15, 0x82, 0x2B); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        COMlabel1.Background = brush;
                    });
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0x15, 0x82, 0x2B); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        COMlabel.Background = brush;
                    });
                    break;
                case 2001:
                    Dispatcher.Invoke(() => COMlabel.Content = "INACTIVE");
                    Dispatcher.Invoke(() => COMlabel1.Content = "INACTIVE");
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0xCA, 0x25, 0x0A); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        COMlabel1.Background = brush;
                    });
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0xCA, 0x25, 0x0A); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        COMlabel.Background = brush;
                    });
                    break;
                case 2003:
                    Dispatcher.Invoke(() => COMlabel.Content = "INACTIVE");
                    Dispatcher.Invoke(() => COMlabel1.Content = "INACTIVE");
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0xCA, 0x25, 0x0A); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        COMlabel1.Background = brush;
                    });
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0xCA, 0x25, 0x0A); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        COMlabel.Background = brush;
                    });
                    break;
                default:
                    // Handle any other status code
                    break;

            }

        }
        public void checkTcpIpHeatBeat()
        {
            TransactionDriveHeart tcplistner = new TransactionDriveHeart();
            obj.checkTcpComStatus(tcplistner);
            
            switch (statusCode)
            {
                case 1001:
                    Dispatcher.Invoke(() => TCPHeartBtn.Content = "INACTIVE");
                    Dispatcher.Invoke(() => TCPHeartBtn1.Content = "INACTIVE");
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0xCA, 0x25, 0x0A); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        TCPHeartBtn.Background = brush;
                    });
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0xCA, 0x25, 0x0A); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        TCPHeartBtn1.Background = brush;
                    });

                    break;
                case 1003:
                    Dispatcher.Invoke(() => TCPHeartBtn.Content = "INACTIVE");
                    Dispatcher.Invoke(() => TCPHeartBtn1.Content = "INACTIVE");
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0xCA, 0x25, 0x0A); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        TCPHeartBtn.Background = brush;
                    });
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0xCA, 0x25, 0x0A); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        TCPHeartBtn1.Background = brush;
                    });

                    break;
                case 1000:
                    Dispatcher.Invoke(() => TCPHeartBtn.Content = "ACTIVE");
                    Dispatcher.Invoke(() => TCPHeartBtn1.Content = "ACTIVE");
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0x15, 0x82, 0x2B); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        TCPHeartBtn.Background = brush;
                    });
                    Dispatcher.Invoke(() =>
                    {
                        Color color = Color.FromArgb(0xFF, 0x15, 0x82, 0x2B); // ARGB values for the color
                        SolidColorBrush brush = new SolidColorBrush(color);
                        TCPHeartBtn1.Background = brush;
                    });
                    break;
                default:

                    break;

            }

        }
        private void Connect(object sender, RoutedEventArgs e)
        {
            if (tcpComSelect.ToString() == "TCP/IP")
            {
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
                dispatcherTimer.Start();
            }

            btnConnect.IsEnabled = false;
            hostIp = txtIpAddress.Text;
            port = txtPort.Text;
            PortCom = PORTCOM.Text;
            string result = "";
            foreach (char c in PortCom)
            {
                if (char.IsDigit(c))
                {
                    result += c;
                }
            }
            IPortCom = result;
            baudRateCom = BAUDSPEED.Text;
            parityCom = PARITY.Text;
            dataBitsCom = int.Parse(DATABITS.Text);
            stopBitsCom = int.Parse(STOPBITS.Text);
            fullScreen.IsEnabled = false;
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
        }
        private void Autodiscover()
        {

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += work_auto;

            worker.RunWorkerAsync();

            //obj.AutoConnect();
        }

        private void ScanOnlineDevices()
        {

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            TcpIpList.ItemsSource = comdata;
            worker.DoWork += work_onlinedevices_scan;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
        }
        private void OnlineTrans()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += work_onlineTrans_scan;
            worker.RunWorkerAsync();
        }
        ConnectionService obj = new ConnectionService();
        TransactionService trxobj = new TransactionService();

        ConnectionListener comListener = new ConnectionListener();
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {

            if (statusSerialConn)
            {
                //bool status = obj.checkComConn();
                if (true)
                {
                    MessageBox.Show("Serial Cable Disconncected, Please connect serial cable");
                    //resultDisconnect = obj.doCOMDisconnection();
                    statusSerialConn = false;
                    dispatcherTimer.Tick -= new EventHandler(dispatcherTimer_Tick);
                    dispatcherTimer.Stop();

                    if (resultDisconnect == 0)
                    {
                        MessageBox.Show(Application.Current.MainWindow, "Disconnected Successfully");
                        ConnectL.Content = "Not Connected";
                        //ConnectImg.Source = null;
                        //txtIpAddress.Text = "";
                        // txtPort.Text = "";
                        BUFFERSEND.Text = "";
                        BUFFERRECEIVED.Text = "";
                        // PORTCOM.Items.Remove(deviceName);
                        CASHBACKT.Text = "";
                        AMOUNTT.Text = "";
                        RRNT.Text = "";
                        DateT.Text = "";
                        OACT.Text = "";
                        //PORTCOM.Text = "";
                        TRANSTYPE.IsEnabled = false;
                        // DoTrans.IsEnabled = false;
                        // ResponseRecived.Text = "";
                        TRANSTYPEL.IsEnabled = false;
                        CASHBACKL.IsEnabled = false;

                        AMOUNTT.IsEnabled = false;
                        RRNL.IsEnabled = false;
                        RRNT.IsEnabled = false;
                        DateL.IsEnabled = false;
                        DateT.IsEnabled = false;
                        OACL.IsEnabled = false;
                        OACT.IsEnabled = false;
                        chkEtp.IsEnabled = false;
                        rdbCom.IsEnabled = true;
                        rdbTcp.IsEnabled = true;
                        PORTCOM.IsEnabled = true;
                        txtPort.IsEnabled = false;
                        txtIpAddress.IsEnabled = false;
                        SetSetting.IsEnabled = false;
                        chkEcrref.IsEnabled = false;
                        txtEcrref.IsEnabled = false;
                        CashRegisterNumberT.IsEnabled = false;
                        myBrowser.Navigate((Uri)null);
                        btnDisConnect.IsEnabled = false;
                        btnConnect.IsEnabled = true;
                        enter.IsEnabled = false;
                        TRANSTYPE.SelectedIndex = 0;
                        AdminTransaction.SelectedIndex = 0;
                        AdminTransactionL.IsEnabled = false;
                        AdminTransaction.IsEnabled = true;
                        grpTcpSetting.IsEnabled = false;
                        rdbCom.IsChecked = false;
                        rdbTcp.IsChecked = false;
                        BAUDSPEED.SelectedIndex = 5;
                        PARITY.SelectedIndex = 0;
                        DATABITS.SelectedIndex = 3;
                        STOPBITS.SelectedIndex = 0;
                        result = 1;
                        this.ProgBar.IsIndeterminate = false;
                        ProgBar.Visibility = Visibility.Hidden;
                        ProgBarL.Visibility = Visibility.Hidden;
                        txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                        txtIpAddress.Focus();
                        txtPort.Focus();
                    }
                    else
                    {
                        MessageBox.Show(Application.Current.MainWindow, "Try Again");
                    }
                    statusSerialConn = false;
                }
            }
            else if (!statusSerialConn)
            {

                // var result = obj.DeviceConnected(int.Parse(IPortCom), comListener);
                //
                //  bool status = obj.checkComConn();
                //if (status)
                //{
                //    MessageBox.Show("Serial Cable Connected.");

                //    statusSerialConn = true;
                //}
            }
        }
        void work_auto(object sender, DoWorkEventArgs e)
        {
            bool checkNullValidation = true;
            ConnectionService obj = new ConnectionService();
            (sender as BackgroundWorker).ReportProgress(0);
            try
            {

                // obj.DoAutoTCPIPConnection();

                // result = objc.AutoTCPIPConnection(AutohostIp, 8081);


            }
            catch (Exception exe)
            {
                (sender as BackgroundWorker).ReportProgress(1);
                MessageBox.Show(exe.Message);

            }
        }
        void work_onlinedevices_scan(object sender, DoWorkEventArgs e)
        {
            bool checkNullValidation = true;
            ConnectionService obj = new ConnectionService();

            (sender as BackgroundWorker).ReportProgress(0);
            try
            {

                ScanLisnter deviceslisnter = new ScanLisnter();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    comdata.Clear();
                    serialdevice.Clear();
                    serialdevicelist.Clear();
                });
                ConfigData fetchData = new ConfigData();
                obj.getConfiguration(out fetchData);
                fetchData.isAppidle = false;
                obj.setConfiguration(fetchData);
                
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                while (stopwatch.Elapsed.TotalSeconds < 35) // Run for a maximum of 36 seconds
                {
                    obj.scanSerialDevice(deviceslisnter);
                    obj.scanOnlinePOSDevice(deviceslisnter);
                }

                stopwatch.Stop();
                if (serialdevice.Count <= 0)
                {
                    (sender as BackgroundWorker).ReportProgress(40);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TcpIpList.Visibility = Visibility.Hidden;
                        ComList.Visibility = Visibility.Hidden;
                    });

                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {

                        enter.IsEnabled = false;
                        serialdevicelist = serialdevice;
                        if (comdata.Count > 0)
                        {
                            comdata.Clear();
                        }
                        for (int i = 0; i < serialdevicelist.Count; i++)
                        {
                            DeviceList value = new DeviceList();
                            value = serialdevicelist[i];

                            if (comdata.Any(c => c.deviceIp == value.deviceIp) == false)
                            {
                                comdata.Add(value);
                            }
                        }
                        serialdevice.Clear();
                        serialdevicelist.Clear();
                        TcpIpList.Visibility = Visibility.Visible;
                        obj.getConfiguration(out fetchData);
                        fetchData.isAppidle = true;
                        obj.setConfiguration(fetchData);

                    });
                    (sender as BackgroundWorker).ReportProgress(10);

                }
            }
            catch (SocketException se)
            {
                (sender as BackgroundWorker).ReportProgress(1);
                obj.getConfiguration(out fetchData);
                fetchData.isAppidle = true;
                obj.setConfiguration(fetchData);
                MessageBox.Show("A socket error occurred: " + se.Message);
            }
        }
        static string resp = "";
        public class TransactionDrive : ITransactionListener
        {
            public void OnFailure(string errorMsg, int errorCode)
            {
                MessageBox.Show(errorMsg, errorCode.ToString());



            }

            public void OnNext(string action)
            {
                throw new NotImplementedException();
            }

            public void OnSuccess(string paymentResponse)
            {
                resp = paymentResponse;
            }
        }
        static string Amount = "";
        static string requestbody = "";
        void work_onlineTrans_scan(object sender, DoWorkEventArgs e)
        {

            bool checkNullValidation = true;

            ConnectionService obj = new ConnectionService();
            (sender as BackgroundWorker).ReportProgress(0);
            try
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ResponseReceive.Text = " ";
                        resp = " ";
                    });
                    if (transTypeSelectedPos != null)
                    {
                        if (transTypeSelectedPos.ToString() == TxnConstant.SALE)
                        {
                            transactionType = "4001";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.UPI_SALE_REQUEST)
                        {
                            transactionType = "5120";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.BHARAT_QR_SALE_RQUEST)
                        {
                            transactionType = "5123";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.REFUND)
                        {
                            transactionType = "4002";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.SALE_COMPLETE)
                        {
                            transactionType = "4008";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.VOID)
                        {
                            transactionType = "4006";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.TIP_ADJUST)
                        {
                            transactionType = "4015";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.ADJUST)
                        {
                            transactionType = "4005";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.SETTLEMENT)
                        {
                            transactionType = "6001";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.BRAND_EMI)
                        {
                            transactionType = "5002";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.BANK_EMI)
                        {
                            transactionType = "5101";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.CASH_ONLY)
                        {
                            transactionType = "4503";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.PAYBACK_VOID)
                        {
                            transactionType = "4403";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.PAYBACK_EARN)
                        {
                            transactionType = "4404";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.SALE_WITH_CASH)
                        {
                            transactionType = "4502";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.PRE_AUTH_TXN)
                        {
                            transactionType = "4007";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.WALLETPAY)
                        {
                            transactionType = "5102";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.WALLETLAOD)
                        {
                            transactionType = "5103";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.WALLETVOID)
                        {
                            transactionType = "5104";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.TWID_SALE)
                        {
                            transactionType = "5131";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.TWID_GET)
                        {
                            transactionType = "5122";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.TWID_VOID)
                        {
                            transactionType = "5121";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.AmazonPayBarcode)
                        {
                            transactionType = "5129";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.EMI_as_Single_Txn_Type_for_Brand_and_Bank_EMI)
                        {
                            transactionType = "5505";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.SALE_CARD_BRAND_EMI)
                        {
                            transactionType = "5003";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.QC_Redemption)
                        {
                            transactionType = "4205";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.Gift_Code)
                        {
                            transactionType = "4113";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.Void_Cardless_Bank)
                        {
                            transactionType = "5031";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.Sale_With_Without_Instant_Discount)
                        {
                            transactionType = "4603";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.Magic_Pin_Sale)
                        {
                            transactionType = "4109";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.Magic_PIN_Void)
                        {
                            transactionType = "4110";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.Zest_MoNAy_Invoice_Sale)
                        {
                            transactionType = "5367";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.Zest_MoNAy_Product_Sale)
                        {
                            transactionType = "5370";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.Zest_MoNAy_Void)
                        {
                            transactionType = "5369";
                        }
                        else if (transTypeSelectedPos.ToString() == TxnConstant.HDFC_Flexipay)
                        {
                            transactionType = "5030";
                        }




                    }
                    else
                    {
                        MessageBox.Show("Transaction Type is Null/Empty", "2003");
                    }
                }
                catch (NullReferenceException ex)
                {
                    MessageBox.Show("Please select Txn type");
                }
                TransactionDrive transactionDrive = new TransactionDrive();
                if (transactionType != "")
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Amount = AMOUNTT.Text;
                    });

                    if (Amount != "")
                    {
                        requestbody = Amount;
                        string afterreplace = requestbody.Replace("10000", Amount);
                        trxobj.doTransaction(afterreplace, int.Parse(transactionType), transactionDrive);
                        obj.getConfiguration(out fetchData);
                        fetchData.isAppidle = false;
                        obj.setConfiguration(fetchData);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (resp != "")
                            {
                                ResponseReceive.Text = resp;
                            }
                            //Requestsend.Text = RemoveNewlines(trxobj._transactionRequestBody);
                            Btnprocess.IsEnabled = true;
                          

                        });
                        Thread.Sleep(10000);
                        obj.getConfiguration(out fetchData);
                        fetchData.isAppidle = true;
                        obj.setConfiguration(fetchData);


                    }
                    else
                    {
                        MessageBox.Show("Amount is Null/Empty", "2001");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Btnprocess.IsEnabled = true;
                        });
                    }
                }
                else
                {
                    Console.WriteLine("Transaction Type is Null/Empty", "2003");
                }



            }
            catch (NullReferenceException nullException)
            {
                MessageBox.Show("Please select Txn Type");
            }
            catch (SocketException se)
            {
                (sender as BackgroundWorker).ReportProgress(1);
                MessageBox.Show("Transaction failed due to Terminal Problem", "1003");
            }
            catch (TimeoutException ex)
            {
                (sender as BackgroundWorker).ReportProgress(2);
                MessageBox.Show("An error occurred: " + ex.Message);
            }
           
        }
        public string RemoveNewlines(string input)
        {
            return input.Replace("\r", "").Replace("\n", "");
        }

        void work_Comdevices_scan(object sender, DoWorkEventArgs e)
        {
            bool checkNullValidation = true;
            ConnectionService obj = new ConnectionService();


            (sender as BackgroundWorker).ReportProgress(0);
            try
            {

                ScanLisnter deviceslisnter = new ScanLisnter();

                obj.scanSerialDevice(deviceslisnter);


                if (serialdevice.Count <= 0)
                {

                    (sender as BackgroundWorker).ReportProgress(2);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TcpIpList.Visibility = Visibility.Hidden;
                        ComList.Visibility = Visibility.Hidden;
                    });
                }
                else if (serialdevice.Count == 1)
                {
                    (sender as BackgroundWorker).ReportProgress(143);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TcpIpList.Visibility = Visibility.Hidden;
                        ComList.Visibility = Visibility.Hidden;

                        btnDisConnect.IsEnabled = true;
                        enter.IsEnabled = true;
                        AMOUNTT.IsEnabled = true;
                        serialdevice.Clear();
                        serialdevicelist.Clear();

                        ConnectL.Content = "Connected";
                    });
                }
                else
                {
                    (sender as BackgroundWorker).ReportProgress(10);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        enter.IsEnabled = false;
                        serialdevicelist = serialdevice;
                        if (comdata.Count > 0)
                        {
                            comdata.Clear();
                        }
                        for (int i = 0; i < serialdevicelist.Count; i++)
                        {
                            DeviceList value = new DeviceList();
                            value = serialdevicelist[i];
                            comdata.Add(value);
                        }
                        serialdevice.Clear();
                        serialdevicelist.Clear();
                        TcpIpList.Visibility = Visibility.Hidden;
                        ComList.Visibility = Visibility.Visible;
                    });

                }

            }
            catch (SocketException se)
            {
                (sender as BackgroundWorker).ReportProgress(1);
                MessageBox.Show("A socket error occurred: " + se.Message);
            }

        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool checkNullValidation = true;
            (sender as BackgroundWorker).ReportProgress(0);
            try
            {
                ConnectionService obj = new ConnectionService();
                if (tcpComSelect.ToString() == "TCP/IP")
                {
                    if (PortCom.ToString() == string.Empty || baudRateCom.ToString() == string.Empty || parityCom.ToString() == string.Empty || dataBitsCom.ToString() == string.Empty || stopBitsCom.ToString() == string.Empty)
                    {
                        checkNullValidation = false;
                        (sender as BackgroundWorker).ReportProgress(3);
                    }
                    else
                    {
                        if (result.ToString() == "0")
                        {
                            statusSerialConn = true;
                        }
                    }
                }
                else
                {

                    var parts = hostIp.Split('.');
                    bool isValid = parts.Length == 4
                                   && !parts.Any(
                                       x =>
                                       {
                                           int y;
                                           return Int32.TryParse(x, out y) && y > 255 || y < 0;
                                       });
                    if (hostIp.ToString() == string.Empty)
                    {
                        checkNullValidation = false;
                        (sender as BackgroundWorker).ReportProgress(2);

                    }
                    else if (port.ToString() == string.Empty)
                    {
                        checkNullValidation = false;
                        (sender as BackgroundWorker).ReportProgress(3);
                    }
                    else if (isValid != true)
                    {
                        checkNullValidation = false;
                        (sender as BackgroundWorker).ReportProgress(5);
                    }
                    else
                    {
                        // result = obj.doTCPIPConnection(hostIp, int.Parse(port));
                    }

                }
                if (checkNullValidation)
                {
                    if (result == 0)
                    {
                        (sender as BackgroundWorker).ReportProgress(1);
                    }
                    else
                    {
                        (sender as BackgroundWorker).ReportProgress(4);
                    }

                }
            }
            catch (Exception ex)
            {
                (sender as BackgroundWorker).ReportProgress(1);
                MessageBox.Show(ex.Message);
            }
        }
        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage.ToString() == "0")
            {
                ProgBarL.Content = "Progress";
                ProgBarL.Visibility = Visibility.Visible;
                ProgBar.Visibility = Visibility.Visible;
                ProgBar.IsIndeterminate = true;
            }
            if (e.ProgressPercentage.ToString() == "10")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;

                btnConnect.IsEnabled = true;
                ProgBar.IsIndeterminate = false;
            }
            if (e.ProgressPercentage.ToString() == "143")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                btnConnect.IsEnabled = true;
                ProgBar.IsIndeterminate = false;
            }
            if (e.ProgressPercentage.ToString() == "40")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                MessageBox.Show("1004", "No Device Found");
                TcpIpList.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                btnConnect.IsEnabled = true;

                ProgBar.IsIndeterminate = false;
            }
            if (e.ProgressPercentage.ToString() == "2")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                btnConnect.IsEnabled = true;
                ComList.Visibility = Visibility.Hidden;
                MessageBox.Show("1002", "No Device Found");
                ProgBar.IsIndeterminate = false;
            }
            if (e.ProgressPercentage.ToString() == "3")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                MessageBox.Show(Application.Current.MainWindow, "PORT Should not be empty");
                btnConnect.IsEnabled = true;
            }
            if (e.ProgressPercentage.ToString() == "4")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                MessageBox.Show(Application.Current.MainWindow, "Problem connecting to Terminal");
                btnConnect.IsEnabled = true;
            }
            if (e.ProgressPercentage.ToString() == "5")
            {
                ProgBarL.Content = "Connecting...";
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                MessageBox.Show(Application.Current.MainWindow, "Ip address not correct Format");
                btnConnect.IsEnabled = true;
            }
            if (e.ProgressPercentage.ToString() == "1")
            {
                ProgBar.IsIndeterminate = false;
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                if (result == 0)
                {
                    BitmapImage bmi = new BitmapImage();
                    bmi.BeginInit();
                    bmi.UriSource = new Uri("img\\connectedIcon.png", UriKind.Relative);
                    bmi.EndInit();
                    // ConnectImg.Stretch = Stretch.Fill;
                    //ConnectImg.Source = bmi;
                    ConnectL.Content = "Connected";
                    rdbCom.IsEnabled = false;
                    rdbTcp.IsEnabled = false;
                    CashRegisterNumberL.IsEnabled = false;
                    CashRegisterNumberT.IsEnabled = false;
                    btnConnect.IsEnabled = false;
                    btnDisConnect.IsEnabled = true;
                    enter.IsEnabled = true;
                    TRANSTYPE.IsEnabled = true;
                    TRANSTYPEL.IsEnabled = true;
                    AdminTransaction.IsEnabled = true;
                    AdminTransactionL.IsEnabled = true;
                    txtPort.IsEnabled = false;
                    PORTCOM.IsEnabled = false;
                    txtIpAddress.IsEnabled = false;
                    BAUDSPEED.IsEnabled = false;
                    PARITY.IsEnabled = false;
                    DATABITS.IsEnabled = false;
                    STOPBITS.IsEnabled = false;
                    int number = 1;
                    txtEcrref.Text = number.ToString("D6");
                    CashRegisterNumberL.Focus();
                }
                else
                {
                    btnConnect.IsEnabled = true;
                }
            }
        }
        private void Enter(object sender, RoutedEventArgs e)
        {
            BUFFERSEND.Text = "";
            BUFFERRECEIVED.Text = "";
            myBrowser.Navigate((Uri)null);
            AMOUNT = AMOUNTT.Text.ToString();
            CASHBACK = CASHBACKT.Text.ToString();
            string validateMsg = "";
            bool flag = false;
            bool flagDate = true;
            string firstDigitAmount = string.Empty;
            string firstDigitCashback = string.Empty;
            var date = string.Empty;
            if (AMOUNT.Length == 0)
            {
                flag = true;
            }
            else if (Convert.ToDecimal(AMOUNT) <= 0)
            {
                firstDigitCashback = "0";
                validateMsg = "Amount";
            }
            if (CASHBACK.Length == 0)
            {
                flag = true;
            }
            else if (Convert.ToDecimal(CASHBACK) <= 0)
            {
                firstDigitCashback = "0";
                if (firstDigitCashback.ToString() == "0")
                {
                    validateMsg = "Cashback";
                }
            }
            if (DateT.Text != "")
            {
                date = DateT.SelectedDate.Value.ToString("dd MM yy");
                originalTransactionDate = date.Replace(" ", "");
                string a = DateTime.Parse(DateT.Text).ToString();
                string b = DateTime.Now.ToString();
                if (DateTime.Parse(a) > DateTime.Parse(b) && TRANSTYPE.Text.ToString() == "Brand EMI")
                {
                    flagDate = false;
                }
            }
            if (TRANSTYPE.Text.ToString() != "" || AdminTransaction.Text.ToString() != "")
            {
                if (TRANSTYPE.Text.ToString() != "")
                {
                    transTypeSelected = TRANSTYPE.Text.ToString();
                }
                else
                {
                    transTypeSelected = AdminTransaction.Text.ToString();
                }
                if (flagDate)
                {
                    if (txtEcrref.Text.ToString() == "000000")
                    {
                        MessageBox.Show(Application.Current.MainWindow, "Please Enter the valid ECR Reference Number");
                    }
                    else
                    {
                        if (firstDigitAmount.ToString() != "0" && firstDigitCashback.ToString() != "0")
                        {
                            ComboBoxItem ComboItem = (ComboBoxItem)TRANSTYPE.SelectedItem;
                            name = ComboItem.Name;
                            printCheck = chkEtp.IsChecked ?? false;
                            try
                            {
                                string projectDirectory = "C:\\ECRSkyband\\CashRegister.xml";
                                using (XmlReader reader = XmlReader.Create(projectDirectory))
                                {
                                    bool checkStatus = false;
                                    while (reader.Read())
                                    {
                                        switch (reader.NodeType)
                                        {
                                            case XmlNodeType.Element:
                                                {
                                                    if (reader.Name.ToString() == "cashRegister")
                                                    {
                                                        checkStatus = true;
                                                    }
                                                    break;
                                                }
                                            case XmlNodeType.Text:
                                                {
                                                    if (checkStatus)
                                                    {
                                                        sendData = reader.Value.ToString();
                                                        checkStatus = false;
                                                    }
                                                    break;
                                                }
                                        }
                                    }
                                }
                                if (sendData != null)
                                {
                                    if (AMOUNT.Length != 0)
                                    {
                                        int amt = (int)(Convert.ToDecimal(AMOUNT) * 100);
                                        AMOUNT = amt.ToString();
                                    }
                                    if (CASHBACK.Length != 0)
                                    {
                                        int cashAmt = (int)(Convert.ToDecimal(CASHBACK) * 100);
                                        CASHBACK = cashAmt.ToString();
                                    }
                                    cashRegister = sendData.Trim();
                                    CashRegisterNumberT.Text = cashRegister;
                                    CashRegisterNumber = CashRegisterNumberT.Text.ToString();
                                    ECRREFtext = txtEcrref.Text.ToString();
                                    RRN = RRNT.Text.ToString();
                                    OAC = OACT.Text.ToString();
                                    BUFFERSENDtext = BUFFERSEND.Text.ToString();
                                    BUFFERRECEIVEDtext = BUFFERRECEIVED.Text.ToString();
                                    VendorID = VendorIDT.Text.ToString();
                                    TRSMID = TRSMIDT.Text.ToString();
                                    VendorTerminaltype = VendorTerminaltypeT.Text.ToString();
                                    VendorKeyIndex = VendorKeyIndexT.Text.ToString();
                                    // SAMAKeyIndex = SAMAKeyIndexT.Text.ToString();
                                    BillerID = BillerIDT.Text.ToString(); ;
                                    BillNumber = BillNumberT.Text.ToString();
                                    prvsECRNumber = prvsECRNumberT.Text.ToString();
                                    ECRrefNum = chkEcrref.IsChecked ?? false;
                                    enter.IsEnabled = false;
                                    fullScreen.IsEnabled = false;
                                    workerSend = new BackgroundWorker();
                                    workerSend.WorkerReportsProgress = true;
                                    workerSend.DoWork += worker_DoWorkSend;
                                    workerSend.ProgressChanged += worker_ProgressSend;
                                    workerSend.RunWorkerAsync();
                                }
                                else
                                {
                                    MessageBox.Show(Application.Current.MainWindow, "Cash register number required in Xml file");
                                }
                            }
                            catch (Exception Ex)
                            {
                                Console.WriteLine("The following exception was raised: ");
                                MessageBox.Show(Application.Current.MainWindow, Ex.Message);
                            }
                        }
                        else
                        {
                            MessageBox.Show(Application.Current.MainWindow, "" + validateMsg + " should not start with zero");
                        }
                    }
                }
                else
                {
                    MessageBox.Show(Application.Current.MainWindow, "Date should not greater than today's date");
                }
            }
            else
            {
                MessageBox.Show(Application.Current.MainWindow, "Please Select one Transaction");
            }
        }
        void worker_DoWorkSend(object sender, DoWorkEventArgs e)
        {
            (sender as BackgroundWorker).ReportProgress(0);
            int responseInt = 1;
            DateTime now = DateTime.Now;

            string dateTime = now.ToString("dd MM yy HH:mm:ss");
            dateTime = dateTime.Replace(" ", "").Replace(":", "");
            string year = now.ToString("dd MM yyyy");
            year = year.Replace(" ", "");
            year = year.Substring(4, 4);
            bool checkNullValidation = true;
            // 
            if (printCheck)
            {
                etpInput = "1";
            }
            else etpInput = "0";
            ECRREferenceNumber = String.Concat(CashRegisterNumber, ECRREFtext);
            //register-----------------------------------------------------------------
            if (transTypeSelected.ToString() == "Register")
            {
                if (CashRegisterNumber == string.Empty)
                {
                    checkNullValidation = false;
                    messageShow("Cash register number Should not empty");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
                else if (CashRegisterNumber.Length != 8)
                {
                    checkNullValidation = false;
                    messageShow("Cash register number Should be 8 digits");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
                else
                {
                    transactionType = "17";
                    inputReqData = dateTime + ";" + CashRegisterNumber;
                }
            }
            //Start Session---------------------------------------------------------------
            if (transTypeSelected.ToString() == "Start Session")
            {
                if (TerminalId != "")
                {
                    if (CashRegisterNumber == string.Empty)
                    {
                        checkNullValidation = false;
                        messageShow("Cash register number Should not empty");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else if (CashRegisterNumber.Length != 8)
                    {
                        checkNullValidation = false;
                        messageShow("Cash register number Should be 8 digits");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else
                    {
                        transactionType = "18";
                        inputReqData = dateTime + ";" + CashRegisterNumber;
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            //End Session---------------------------------------------------------------
            if (transTypeSelected.ToString() == "End Session")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (CashRegisterNumber == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Cash register number Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (CashRegisterNumber.Length != 8)
                        {
                            checkNullValidation = false;
                            messageShow("Cash register number Should be 8 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "19";
                            inputReqData = dateTime + ";" + CashRegisterNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }

                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            // purchase-------------------------------------------------------------------
            if (transTypeSelected.ToString() == "Purchase")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "0";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }

            if (transTypeSelected.ToString() == "UPI")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }

                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        //else if (Convert.ToDecimal(CASHBACK) >= Convert.ToDecimal(AMOUNT))
                        //{
                        //    checkNullValidation = false;
                        //    messageShow("Cashback Amount should be lesser than Purchase Amount");
                        //    (sender as BackgroundWorker).ReportProgress(2);
                        //}
                        else
                        {
                            transactionType = "1";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + CASHBACK + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            //Brand EMI---------------------------------------------------------------------------------
            if (transTypeSelected.ToString() == "Brand EMI")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        //else if (RRN == string.Empty)
                        //{
                        //    checkNullValidation = false;
                        //    messageShow("RRN Should not empty");
                        //    (sender as BackgroundWorker).ReportProgress(2);
                        //}
                        //else if (originalTransactionDate.ToString() == string.Empty)
                        //{
                        //    checkNullValidation = false;
                        //    messageShow("Date Should not empty");
                        //    (sender as BackgroundWorker).ReportProgress(2);
                        //}
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        //else if (RRN.Length != 12)
                        //{
                        //    checkNullValidation = false;
                        //    messageShow("RRN length should be 12 digits");
                        //    (sender as BackgroundWorker).ReportProgress(2);
                        //}
                        else
                        {
                            transactionType = "2";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + RRN + ";" + etpInput + ";" + originalTransactionDate + ";" + ECRREferenceNumber;
                            originalTransactionDate = "";
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Authorization")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "3";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Authorization Extension")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (RRN == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("RRN Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (originalTransactionDate.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("original Transaction Date Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Original aproval code Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN.Length != 12)
                        {
                            checkNullValidation = false;
                            messageShow("RRN length should be 12 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code length should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "5";
                            inputReqData = dateTime + ";" + RRN + ";" + originalTransactionDate + ";" + OAC + ";" + etpInput + ";" + ECRREferenceNumber;
                            originalTransactionDate = "";
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Authorization Void")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (originalTransactionDate.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("original Transaction Date Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("RRN Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN.Length != 12)
                        {
                            checkNullValidation = false;
                            messageShow("RRN length should be 12 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code length should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "6";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + RRN + ";" + originalTransactionDate + ";" + OAC + ";" + etpInput + ";" + ECRREferenceNumber;
                            originalTransactionDate = "";
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Purchase Advice Full")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("RRN Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (originalTransactionDate.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("original Transaction Date Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN.Length != 12)
                        {
                            checkNullValidation = false;
                            messageShow("RRN length should be 12 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code length should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "4";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + RRN + ";" + originalTransactionDate + ";" + OAC + ";" + "1" + ";" + etpInput + ";" + ECRREferenceNumber;
                            originalTransactionDate = "";
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Purchase Advice Partial")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("RRN Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (originalTransactionDate.ToString() == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("original Transaction Date Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (RRN.Length != 12)
                        {
                            checkNullValidation = false;
                            messageShow("RRN length should be 12 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (OAC.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Original Approval Code length should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "4";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + RRN + ";" + originalTransactionDate + ";" + OAC + ";" + "0" + ";" + etpInput + ";" + ECRREferenceNumber;
                            originalTransactionDate = "";
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Cash Advance")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Amount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "8";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Reversal")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "9";
                            inputReqData = dateTime + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Reconciliation")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "10";
                            inputReqData = dateTime + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Full Download")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "11";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Set Settings")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (VendorID == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("VendorID Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorTerminaltype == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Vendor Terminal type  Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (TRSMID == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("TRSMID Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorKeyIndex == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Vendor Key Index Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (SAMAKeyIndex == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("SAMA Key Index Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorID.Length.ToString() != "2")
                        {
                            checkNullValidation = false;
                            messageShow("Vendor ID should accept only 2 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorTerminaltype.Length.ToString() != "2")
                        {
                            checkNullValidation = false;
                            messageShow("Vendor Terminal type should accept only 2 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (VendorKeyIndex.Length.ToString() != "2")
                        {
                            checkNullValidation = false;
                            messageShow("Vendor Key Index should accept only 2 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (SAMAKeyIndex.Length.ToString() != "2")
                        {
                            checkNullValidation = false;
                            messageShow("SAMA Key Index should accept only 2 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (TRSMID.Length.ToString() != "6")
                        {
                            checkNullValidation = false;
                            messageShow("TRSM ID should accept only 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "12";
                            inputReqData = dateTime + ";" + VendorID + ";" + VendorTerminaltype + ";" + TRSMID + ";" + VendorKeyIndex + ";" + SAMAKeyIndex + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Get Settings")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "13";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Bill Payment")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (AMOUNT == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("BillPaymentAmount Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (BillerID == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("BillerID Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (BillNumber == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("BillNumber Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (BillerID.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("BillerID Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (BillNumber.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("BillNumber Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "20";
                            inputReqData = dateTime + ";" + AMOUNT + ";" + BillerID + ";" + BillNumber + ";" + etpInput + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Running Total")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "21";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Print Summary Report")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "22";
                            inputReqData = dateTime + ";" + 0 + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }

            }
            if (transTypeSelected.ToString() == "Duplicate")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (prvsECRNumber == "000000")
                        {
                            checkNullValidation = false;
                            messageShow("previous ecr reference number not valid");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (prvsECRNumber == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("previous ECR Number Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (prvsECRNumber.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("previous ECR Number Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "23";
                            inputReqData = dateTime + ";" + prvsECRNumber + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
            if (transTypeSelected.ToString() == "Check Status")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "24";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }

            }
            if (transTypeSelected.ToString() == "Partial Download")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "25";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }

            }
            if (transTypeSelected.ToString() == "Snapshot Total")
            {
                if (TerminalId != "")
                {
                    if (status == 1)
                    {
                        if (ECRREFtext == string.Empty)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should not empty");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else if (ECRREFtext.Length != 6)
                        {
                            checkNullValidation = false;
                            messageShow("Ecr Ref Should be 6 digits");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            transactionType = "26";
                            inputReqData = dateTime + ";" + ECRREferenceNumber;
                        }
                    }
                    else
                    {
                        checkNullValidation = false;
                        messageShow("Session not started");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                }
                else
                {
                    checkNullValidation = false;
                    messageShow("Please Register First");
                    (sender as BackgroundWorker).ReportProgress(2);
                }

            }
            ecrReferenceNo = ECRREFtext;
            if (TerminalId != "")
            {
                szSignature = String.Concat(ecrReferenceNo, TerminalId);
            }
            string response = "ERROR";
            if (checkNullValidation)
            {
                try
                {
                    if (inputReqData != "")
                    {
                        inputReqData = inputReqData + "!";
                        BUFFERSENDtext = inputReqData;
                        (sender as BackgroundWorker).ReportProgress(3);
                    }
                    ConnectionService obj = new ConnectionService();
                    // szSignature = obj.ComputeSha256Hash(szSignature);
                    //counting ecr teferebce number-----------------
                    if (transactionType != "17" && transactionType != "18" && transactionType != "19")
                    {
                        int refNumber = int.Parse(ECRREFtext) + 1;
                        ECRREFtext = refNumber.ToString("D6");
                        (sender as BackgroundWorker).ReportProgress(4);
                    }
                    string res = "";

                    if (tcpComSelect.ToString() == "TCP/IP")
                    {
                        responseInt = 0;//obj.doCOMTransaction(int.Parse(IPortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom,
                                        // inputReqData, int.Parse(transactionType), szSignature, out res);
                        string responseSummary;
                        string[] sep = { ";" };
                        string[] responseTemp = new string[1];
                        string[] responseTemp1 = new string[1];
                        if (transactionType == "22")
                        {
                            string[] resp = res.Split(sep, StringSplitOptions.None);
                            string responseCommand = resp[1];
                            int count = int.Parse(resp[5]);
                            responseTemp1 = new string[resp.Length - 2];
                            int m = 1;
                            for (int n = 1; n < resp.Length - 2; n++)
                            {
                                responseTemp1[m] = resp[n];
                                m = m + 1;
                            }
                            res = "";
                            res = ConvertStringArrayToString(responseTemp1);
                            if (count > 0)
                            {
                                responseSummary = ConvertStringArrayToString(responseTemp1);
                                for (int i = 1; i <= count; i++)
                                {
                                    res = "";
                                    ECRREferenceNumber = String.Concat(CashRegisterNumber, ECRREFtext);
                                    ecrReferenceNo = ECRREFtext;
                                    dateTime = now.ToString("dd MM yy HH:mm:ss");
                                    dateTime = dateTime.Replace(" ", "").Replace(":", "");
                                    inputReqData = dateTime + ";" + i + ";" + ECRREferenceNumber + "!";
                                    szSignature = String.Concat(ecrReferenceNo, TerminalId);
                                    // szSignature = obj.ComputeSha256Hash(szSignature);
                                    responseInt = 0;// obj.doCOMTransaction(int.Parse(IPortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom, inputReqData, int.Parse(transactionType), szSignature, out res);
                                    if (responseInt == 0)
                                    {
                                        string[] resp1 = res.Split(sep, StringSplitOptions.None);
                                        responseTemp = new string[resp1.Length - 6];
                                        int j = 0;
                                        for (int k = 4; k < resp1.Length - 2; k++)
                                        {
                                            responseTemp[j] = resp1[k];
                                            j = j + 1;
                                        }
                                        res = "";
                                        res = ConvertStringArrayToString(responseTemp);
                                        responseSummary = String.Concat(responseSummary, res);
                                        int refNumber1 = int.Parse(ECRREFtext) + 1;
                                        ECRREFtext = refNumber1.ToString("D6");
                                        (sender as BackgroundWorker).ReportProgress(4);
                                    }
                                }
                                res = responseSummary;
                            }
                        }
                    }
                    else
                    {
                        responseInt = 0;//obj.doTCPIPTransaction(hostIp, int.Parse(port), inputReqData,
                                        // int.Parse(transactionType), szSignature, out res);
                        string responseSummary;
                        string[] sep = { ";" };
                        string[] responseTemp = new string[1];
                        string[] responseTemp1 = new string[1];
                        if (transactionType == "22")
                        {
                            string[] resp = res.Split(sep, StringSplitOptions.None);
                            string responseCommand = resp[1];
                            int count = int.Parse(resp[5]);
                            responseTemp1 = new string[resp.Length - 2];
                            int m = 1;
                            for (int n = 1; n < resp.Length - 2; n++)
                            {
                                responseTemp1[m] = resp[n];
                                m = m + 1;
                            }
                            res = "";
                            res = ConvertStringArrayToString(responseTemp1);
                            if (count > 0)
                            {
                                responseSummary = ConvertStringArrayToString(responseTemp1);
                                for (int i = 1; i <= count; i++)
                                {
                                    res = "";
                                    ECRREferenceNumber = String.Concat(CashRegisterNumber, ECRREFtext);
                                    ecrReferenceNo = ECRREFtext;
                                    dateTime = now.ToString("dd MM yy HH:mm:ss");
                                    dateTime = dateTime.Replace(" ", "").Replace(":", "");
                                    inputReqData = dateTime + ";" + i + ";" + ECRREferenceNumber + "!";
                                    szSignature = String.Concat(ecrReferenceNo, TerminalId);
                                    // szSignature = obj.ComputeSha256Hash(szSignature);
                                    responseInt = 0;// obj.doTCPIPTransaction(hostIp, int.Parse(port), inputReqData, int.Parse(transactionType), szSignature, out res);
                                    if (responseInt == 0)
                                    {
                                        string[] resp1 = res.Split(sep, StringSplitOptions.None);
                                        responseTemp = new string[resp1.Length - 6];
                                        int j = 0;
                                        for (int k = 4; k < resp1.Length - 2; k++)
                                        {
                                            responseTemp[j] = resp1[k];
                                            j = j + 1;
                                        }
                                        res = "";
                                        res = ConvertStringArrayToString(responseTemp);
                                        responseSummary = String.Concat(responseSummary, res);
                                        int refNumber1 = int.Parse(ECRREFtext) + 1;
                                        ECRREFtext = refNumber1.ToString("D6");
                                        (sender as BackgroundWorker).ReportProgress(4);
                                    }
                                }
                                res = responseSummary;
                            }
                        }
                    }
                    if (responseInt == 0)
                    {
                        string[] sep = { ";" };
                        string[] resp = res.Split(sep, StringSplitOptions.None);
                        string responseCommand = resp[1];
                        response = resp[0];
                        // TranslatorService service = new TranslatorService();
                        var sourceLanguage = "English";
                        var targetLanguage = "Arabic";
                        string pan = "";
                        if (responseCommand == "0" || responseCommand == "1" || responseCommand == "8" || responseCommand == "3" || responseCommand == "9" || responseCommand == "2" || responseCommand == "4" || responseCommand == "5" || responseCommand == "6" || responseCommand == "20" || responseCommand == "22")
                        {
                            if (resp[4].Length > 16)
                            {
                                // pan = obj.MaskDigitsPan(resp[4]);
                            }

                        }
                        if (responseCommand == "18")
                        {
                            if (resp[2] == "00")
                            {
                                status = 1;
                            }
                        }
                        if (responseCommand == "19")
                        {
                            if (resp[2] == "00")
                            {
                                status = 0;
                            }
                        }
                        if (responseCommand == "17")
                        {
                            status = 0;
                            TerminalId = resp[3];
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Terminal id" + "\t" + "\t" + "\t" + ": " + resp[3] + "\n";
                        }
                        else if (responseCommand == "0" && resp.Length > 27)
                        {
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Purchase(customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }
                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", resp[4]);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("kernalId", resp[21]);
                                builder.Replace("PAR", resp[22]);
                                builder.Replace("suffix", resp[23]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[25]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[30]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }

                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                        }


                        else if (responseCommand == "1" && resp.Length > 29)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Cash Back Amount" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Total Amount" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                   "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[34] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[35] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[36] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[37] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[38] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Purchase cashback(customer copy)).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal TransactionAmount = Convert.ToDecimal(resp[5]) / 100;
                                decimal CashbackAmount = Convert.ToDecimal(resp[6]) / 100;
                                decimal TotalAmount = Convert.ToDecimal(resp[7]) / 100;
                                string ExpiryDate = resp[11];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }

                                string respDateTime = resp[10];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSARPUR", numToArabicConverter(string.Format("{0:0.00##}", TransactionAmount.ToString())));
                                builder.Replace("amountSARcashback", numToArabicConverter(string.Format("{0:0.00##}", CashbackAmount.ToString())));
                                builder.Replace("amountSARtotal", numToArabicConverter(string.Format("{0:0.00##}", TotalAmount.ToString())));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[13]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", resp[4]);
                                builder.Replace("Buzzcode", resp[8]);
                                builder.Replace("authCode", resp[13]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("TransactionAmount", (string.Format("{0:0.00##}", TransactionAmount)));
                                builder.Replace("CashbackAmount", (string.Format("{0:0.00##}", CashbackAmount)));
                                builder.Replace("TotalAmount", (string.Format("{0:0.00##}", TotalAmount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[26]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[17]);
                                builder.Replace("TVR", resp[21]);
                                builder.Replace("CVR", resp[20]);
                                builder.Replace("applicationCryptogram", resp[18]);
                                builder.Replace("CID", resp[19]);
                                builder.Replace("MID", resp[15]);
                                builder.Replace("TID", resp[14]);
                                builder.Replace("RRN", resp[12]);
                                builder.Replace("StanNo", resp[9]);
                                builder.Replace("TSI", resp[22]);
                                builder.Replace("kernalId", resp[23]);
                                builder.Replace("PAR", resp[24]);
                                builder.Replace("suffix", resp[25]);
                                builder.Replace("ApplicationVersion", resp[31]);
                                builder.Replace("SchemeLabel", resp[29]);
                                builder.Replace("MerchantCategoryCode", resp[27]);
                                builder.Replace("Merchant Name", resp[33]);
                                builder.Replace("Merchant Address", resp[34]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[35]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[36]));
                                builder.Replace("Disclaimer", resp[32]);
                                string arabicpin = CheckingArabicPin(resp[32]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "8" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Cash_Advance(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }
                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("kernalId", resp[21]);
                                builder.Replace("PAR", resp[22]);
                                builder.Replace("suffix", resp[23]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[25]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[30]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "3" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Pre-Auth(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }
                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("kernalId", resp[21]);
                                builder.Replace("PAR", resp[22]);
                                builder.Replace("suffix", resp[23]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[25]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[30]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "9" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[2] == "400")
                            {
                                string pathToHTMLFile = @"printReceipt\\Reversal(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }
                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("kernalId", resp[21]);
                                builder.Replace("PAR", resp[22]);
                                builder.Replace("suffix", resp[23]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[25]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[30]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "2" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\BrandEMI(customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }
                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("kernalId", resp[21]);
                                builder.Replace("PAR", resp[22]);
                                builder.Replace("suffix", resp[23]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[25]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[30]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }

                        }
                        else if (responseCommand == "4" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Purchase Advice(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }
                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("kernalId", resp[21]);
                                builder.Replace("PAR", resp[22]);
                                builder.Replace("suffix", resp[23]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[25]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[30]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "5" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Pre-Extension(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }
                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("kernalId", resp[21]);
                                builder.Replace("PAR", resp[22]);
                                builder.Replace("suffix", resp[23]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[25]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[30]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "6" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Pre-void(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }
                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("kernalId", resp[21]);
                                builder.Replace("PAR", resp[22]);
                                builder.Replace("suffix", resp[23]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[25]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[30]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "10")
                        {
                            if (resp[2] == "500" || resp[2] == "501")
                            {
                                string pathToHTMLFile = @"printReceipt\\Reconcilation.html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);

                                string summaryToHTMLFilePosTable = @"printReceipt\\PosTable.html";
                                string summaryhtmlStringPosTable = File.ReadAllText(summaryToHTMLFilePosTable);
                                StringBuilder htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);

                                string summaryToHTMLFileMadaHost = @"printReceipt\\madaHostTable.html";
                                string summaryhtmlStringMadaHost = File.ReadAllText(summaryToHTMLFileMadaHost);
                                StringBuilder htmlSummaryReportMadaHost = new StringBuilder(summaryhtmlStringMadaHost);

                                string summaryToHTMLFilePosDetials = @"printReceipt\\PosTerminalDetails.html";
                                string summaryhtmlStringPosDetials = File.ReadAllText(summaryToHTMLFilePosDetials);
                                StringBuilder htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);



                                string SummaryFinalReport = "";
                                string respDateTime = resp[4];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);

                                int b = 9;
                                int totalSchemeLengthL = int.Parse(resp[9]);
                                for (int j = 1; j <= totalSchemeLengthL; j++)
                                {

                                    if (resp[b + 2] == "0")
                                    {
                                        string ReconcilationToHTMLFile = @"printReceipt\\ReconcilationTable.html";
                                        string ReconcilationNoTable = File.ReadAllText(ReconcilationToHTMLFile);
                                        StringBuilder htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                        string arabi = CheckingArabic(resp[b + 1]);
                                        arabi = arabi.Replace("\u08F1", "");

                                        htmlreconcilationNoTable.Replace("Scheme", resp[b + 1]);
                                        htmlreconcilationNoTable.Replace("قَدِيرٞ", arabi);
                                        b = b + 3;

                                        SummaryFinalReport += htmlreconcilationNoTable.ToString();
                                        htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                    }
                                    else
                                    {
                                        if (resp[b + 3] == "mada HOST")
                                        {
                                            j = j - 1;
                                            string arabic12 = CheckingArabic(resp[b + 1]);
                                            arabic12 = arabic12.Replace("\u08F1", "");
                                            htmlSummaryReportMadaHost.Replace("مدى", arabic12);
                                            htmlSummaryReportMadaHost.Replace("schemename", resp[b + 1]);
                                            htmlSummaryReportMadaHost.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 4])).ToString());
                                            htmlSummaryReportMadaHost.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 5]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 6])).ToString());
                                            htmlSummaryReportMadaHost.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 7]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 8])).ToString());
                                            htmlSummaryReportMadaHost.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 9]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("CADVCount", (Convert.ToDecimal(resp[b + 10])).ToString());
                                            htmlSummaryReportMadaHost.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 11]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 12])).ToString());
                                            htmlSummaryReportMadaHost.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 13]) / 100))));
                                            htmlSummaryReportMadaHost.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 14])).ToString());
                                            htmlSummaryReportMadaHost.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 15]) / 100))));
                                            b = b + 15;
                                            SummaryFinalReport += htmlSummaryReportMadaHost.ToString();
                                            htmlSummaryReportMadaHost = new StringBuilder(summaryhtmlStringMadaHost);
                                        }
                                        else if (resp[b + 2] == "POS TERMINAL")
                                        {
                                            j = j - 1;
                                            htmlSummaryReportPosTable.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 3])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 4]) / 100))));
                                            htmlSummaryReportPosTable.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 5])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 6]) / 100))));
                                            htmlSummaryReportPosTable.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 7])).ToString());
                                            htmlSummaryReportPosTable.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 8]) / 100))));
                                            htmlSummaryReportPosTable.Replace("CADVCount", (Convert.ToDecimal(resp[b + 9])).ToString());
                                            htmlSummaryReportPosTable.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 10]) / 100))));
                                            htmlSummaryReportPosTable.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 11])).ToString());
                                            htmlSummaryReportPosTable.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 12]) / 100))));
                                            htmlSummaryReportPosTable.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 13])).ToString());
                                            htmlSummaryReportPosTable.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 14]) / 100))));
                                            b = b + 14;
                                            SummaryFinalReport += htmlSummaryReportPosTable.ToString();
                                            htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);
                                        }
                                        else if (resp[b + 2] == "POS TERMINAL DETAILS")
                                        {
                                            j = j - 1;
                                            htmlSummaryReportPosDetials.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 3])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 4]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 5])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 6]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 7])).ToString());
                                            htmlSummaryReportPosDetials.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 8]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("CADVCount", (Convert.ToDecimal(resp[b + 9])).ToString());
                                            htmlSummaryReportPosDetials.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 10]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 11])).ToString());
                                            htmlSummaryReportPosDetials.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 12]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 13])).ToString());
                                            htmlSummaryReportPosDetials.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 14]) / 100))));
                                            b = b + 14;
                                            SummaryFinalReport += htmlSummaryReportPosDetials.ToString();
                                            htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);
                                        }
                                        else if (resp[b + 1] == "0")
                                        {
                                            string ReconcilationToHTMLFile = @"printReceipt\\ReconcilationTable1.html";
                                            string ReconcilationNoTable = File.ReadAllText(ReconcilationToHTMLFile);
                                            SummaryFinalReport += ReconcilationNoTable;
                                            b = b + 1;
                                        }
                                    }
                                }
                                builder.Replace("PosTable", SummaryFinalReport);
                                builder.Replace("merchantId", resp[5]);
                                builder.Replace("busscode", resp[6]);
                                builder.Replace("traceNumber", resp[7]);
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("AppVersion", resp[8]);
                                builder.Replace("TerminalId", TerminalId);
                                builder.Replace("Merchant Name", resp[b + 1]);
                                builder.Replace("Merchant Address", resp[b + 2]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[b + 3]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[b + 4]));
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));

                                string printSettlment =
                                                   "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                   "Scheme HOST" + "\t" + "\t" + "\t" + ": SchemeHOST \n" +
                                                   "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                   "Total Debit Count" + "\t" + "\t" + "\t" + ": TotalDebitCount \n" +
                                                   "Total Debit Amount" + "\t" + "\t" + ": TotalDebitAmount \n" +
                                                   "Total Credit Count" + "\t" + "\t" + "\t" + ": TotalCreditCount \n" +
                                                   "Total Credit Amount" + "\t" + "\t" + ": TotalCreditAmount \n" +
                                                   "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                   "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                   "C/ADV Count" + "\t" + "\t" + "\t" + ": CADVCount \n" +
                                                   "C/ADV Amount" + "\t" + "\t" + "\t" + ": CADVAmount \n" +
                                                   "Auth Count" + "\t" + "\t" + "\t" + ": AuthCount \n" +
                                                   "Auth Amount" + "\t" + "\t" + "\t" + ": AuthAmount \n" +
                                                   "Total Count" + "\t" + "\t" + "\t" + ": TotalCount \n" +
                                                   "Total Amount" + "\t" + "\t" + "\t" + ": TotalAmount \n";

                                string printSettlmentPos =
                                                        "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                        "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                        "Total Debit Count" + "\t" + "\t" + "\t" + ": TotalDebitCount \n" +
                                                        "Total Debit Amount" + "\t" + "\t" + ": TotalDebitAmount \n" +
                                                        "Total Credit Count" + "\t" + "\t" + "\t" + ": TotalCreditCount \n" +
                                                        "Total Credit Amount" + "\t" + "\t" + ": TotalCreditAmount \n" +
                                                        "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                        "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                        "C/ADV Count" + "\t" + "\t" + "\t" + ": CADVCount \n" +
                                                        "C/ADV Amount" + "\t" + "\t" + "\t" + ": CADVAmount \n" +
                                                        "Auth Count" + "\t" + "\t" + "\t" + ": AuthCount \n" +
                                                        "Auth Amount" + "\t" + "\t" + "\t" + ": AuthAmount \n" +
                                                        "Total Count" + "\t" + "\t" + "\t" + ": TotalCount \n" +
                                                        "Total Amount" + "\t" + "\t" + "\t" + ": TotalAmount \n";
                                string printSettlmentPosDetails =
                                                        "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                        "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                        "P/OFF Count" + "\t" + "\t" + "\t" + ": POFFCount \n" +
                                                        "P/OFF Amount" + "\t" + "\t" + "\t" + ": POFFAmount \n" +
                                                        "P/ON Count" + "\t" + "\t" + "\t" + ": PONCount \n" +
                                                        "P/ON Amount" + "\t" + "\t" + "\t" + ": PONAmount \n" +
                                                        "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                        "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                        "REVERSAL Count" + "\t" + "\t" + "\t" + ": REVERSALCount \n" +
                                                        "REVERSAL Amount" + "\t" + "\t" + ": REVERSALAmount \n" +
                                                        "Brand EMI Count" + "\t" + "\t" + "\t" + ": Brand EMICount \n" +
                                                        "Brand EMI Amount" + "\t" + "\t" + "\t" + ": Brand EMIAmount \n" +
                                                        "COMP Count" + "\t" + "\t" + "\t" + ": COMPCount \n" +
                                                        "COMP Amount" + "\t" + "\t" + "\t" + ": COMPAmount \n";
                                StringBuilder printSettlment1 = new StringBuilder(printSettlment);
                                StringBuilder printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                StringBuilder printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);

                                string printFinalReport1 = "";
                                int k = 9;
                                int totalSchemeLength = int.Parse(resp[9]);
                                for (int i = 1; i <= totalSchemeLength; i++)
                                {

                                    if (resp[k + 2] == "0")
                                    {
                                        string printSettlmentNO = "Scheme Name" + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                                   "<No Transaction> \n";
                                        k = k + 3;
                                        StringBuilder printSettlment2 = new StringBuilder(printSettlmentNO);
                                        printFinalReport1 += printSettlment2.ToString();
                                    }
                                    else
                                    {
                                        if (resp[k + 3] == "mada HOST")
                                        {
                                            i = i - 1;
                                            printSettlment1.Replace("SchemeName", resp[k + 1]);
                                            printSettlment1.Replace("TransactionAvailableFlag", resp[k + 2]);
                                            printSettlment1.Replace("SchemeHOST", resp[k + 3]);
                                            printSettlment1.Replace("TotalDebitCount", resp[k + 4]);
                                            printSettlment1.Replace("TotalDebitAmount", "");
                                            printSettlment1.Replace("TotalCreditCount", resp[k + 6]);
                                            printSettlment1.Replace("TotalCreditAmount", resp[k + 7]);
                                            printSettlment1.Replace("NAQDCount", resp[k + 8]);
                                            printSettlment1.Replace("NAQDAmount", resp[k + 9]);
                                            printSettlment1.Replace("CADVCount", resp[k + 10]);
                                            printSettlment1.Replace("CADVAmount", resp[k + 11]);
                                            printSettlment1.Replace("AuthCount", resp[k + 12]);
                                            printSettlment1.Replace("AuthAmount", resp[k + 13]);
                                            printSettlment1.Replace("TotalCount", resp[k + 14]);
                                            printSettlment1.Replace("TotalAmount", "");
                                            k = k + 15;
                                            printFinalReport1 += printSettlment1.ToString();
                                            printSettlment1 = new StringBuilder(printSettlment);
                                        }
                                        else if (resp[k + 2] == "POS TERMINAL")
                                        {
                                            i = i - 1;
                                            printSettlmentPos1.Replace("TransactionAvailableFlag", resp[k + 1]);
                                            printSettlmentPos1.Replace("SchemeName", resp[k + 2]);
                                            printSettlmentPos1.Replace("TotalDebitCount", resp[k + 3]);
                                            printSettlmentPos1.Replace("TotalDebitAmount", resp[k + 4]);
                                            printSettlmentPos1.Replace("TotalCreditCount", resp[k + 5]);
                                            printSettlmentPos1.Replace("TotalCreditAmount", resp[k + 6]);
                                            printSettlmentPos1.Replace("NAQDCount", resp[k + 7]);
                                            printSettlmentPos1.Replace("NAQDAmount", resp[k + 8]);
                                            printSettlmentPos1.Replace("CADVCount", resp[k + 9]);
                                            printSettlmentPos1.Replace("CADVAmount", resp[k + 10]);
                                            printSettlmentPos1.Replace("AuthCount", resp[k + 11]);
                                            printSettlmentPos1.Replace("AuthAmount", resp[k + 12]);
                                            printSettlmentPos1.Replace("TotalCount", resp[k + 13]);
                                            printSettlmentPos1.Replace("TotalAmount", resp[k + 14]);
                                            k = k + 14;
                                            printFinalReport1 += printSettlmentPos1.ToString();
                                            printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                        }
                                        else if (resp[k + 2] == "POS TERMINAL DETAILS")
                                        {
                                            i = i - 1;
                                            printSettlmentPosDetails1.Replace("TransactionAvailableFlag", resp[k + 1]);
                                            printSettlmentPosDetails1.Replace("SchemeName", resp[k + 2]);
                                            printSettlmentPosDetails1.Replace("POFFCount", resp[k + 3]);
                                            printSettlmentPosDetails1.Replace("POFFAmount", resp[k + 4]);
                                            printSettlmentPosDetails1.Replace("PONCount", resp[k + 5]);
                                            printSettlmentPosDetails1.Replace("PONAmount", resp[k + 6]);
                                            printSettlmentPosDetails1.Replace("NAQDCount", resp[k + 7]);
                                            printSettlmentPosDetails1.Replace("NAQDAmount", resp[k + 8]);
                                            printSettlmentPosDetails1.Replace("REVERSALCount", resp[k + 9]);
                                            printSettlmentPosDetails1.Replace("REVERSALAmount", resp[k + 10]);
                                            printSettlmentPosDetails1.Replace("Brand EMICount", resp[k + 11]);
                                            printSettlmentPosDetails1.Replace("Brand EMIAmount", resp[k + 12]);
                                            printSettlmentPosDetails1.Replace("COMPCount", resp[k + 13]);
                                            printSettlmentPosDetails1.Replace("COMPAmount", resp[k + 14]);
                                            k = k + 14;
                                            printFinalReport1 += printSettlmentPosDetails1.ToString();
                                            printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);
                                        }
                                        else if (resp[k + 1] == "0")
                                        {
                                            string printSettlmentNO1 = "Scheme Name" + "\t" + "\t" + "\t" + ": POS TERMINAL \n" +
                                                                    "<No Transaction> \n" +
                                                                     "Scheme Name" + "\t" + "\t" + "\t" + ": POS TERMINAL Details\n" +
                                                                     "<No Transaction> \n";
                                            k = k + 1;
                                            StringBuilder printSettlment2 = new StringBuilder(printSettlmentNO1);
                                            printFinalReport1 += printSettlment2.ToString();
                                        }
                                    }
                                }
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                     "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                     "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Merchent ID" + "\t" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                     "Trace Number" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                     "Application Version" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                     "Total Scheme Length" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                     printFinalReport1 +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[k + 2] + "\n" +
                                                     "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[k + 3] + "\n" +
                                                      "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[k + 4] + "\n" +
                                                      "ECR Transaction Reference Number" + "\t" + ": " + resp[k + 5] + "\n" +
                                                      "Signature" + "\t" + "\t" + "\t" + ": " + resp[k + 6] + "\n";
                            }
                            else
                            {
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                     "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                     "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "ECR Transaction Reference Number : " + resp[6] + "\n" +
                                                     "Signature" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n";
                            }


                        }
                        else if (responseCommand == "11" && resp.Length > 4)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "ECR Transaction Reference Number : " + resp[5] + "\n" +
                                                 "Signature" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n";
                            if (resp[2] == "300" && resp[3] != "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Parameter download.html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                string respDateTime = resp[4];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("terminalId", TerminalId.Substring(0, 8));
                                builder.Replace("responseCode", resp[2]);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "12" && resp.Length > 4)
                        {

                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "ECR Transaction Reference Number : " + resp[5] + "\n" +
                                                 "Signature" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n";
                        }
                        else if (responseCommand == "13" && resp.Length > 9)
                        {

                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "Vendor ID" + "\t" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                 "Vendor Terminal type" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                 "TRSM ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                 "Vendor Key Index" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                 "SAMA Key Index" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                 "ECR Transaction Reference Number : " + resp[10] + "\n" +
                                                 "Signature" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n";
                        }
                        else if (responseCommand == "18")
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n";
                        }
                        else if (responseCommand == "19")
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n";
                        }
                        else if (responseCommand == "20" && resp.Length > 27)
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                  "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                  "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                  "PAN Number" + "\t" + "\t" + "\t" + ": " + pan + "\n" +
                                                  "Transaction Amount" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                  "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                  "Stan No" + "\t" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                  "Date & Time" + "\t" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                  "Card Exp Date" + "\t" + "\t" + "\t" + ": " + resp[9] + "\n" +
                                                  "RRN" + "\t" + "\t" + "\t" + "\t" + ": " + resp[10] + "\n" +
                                                  "Auth Code" + "\t" + "\t" + "\t" + ": " + resp[11] + "\n" +
                                                  "TID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[12] + "\n" +
                                                  "MID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[13] + "\n" +
                                                  "Batch No" + "\t" + "\t" + "\t" + ": " + resp[14] + "\n" +
                                                  "AID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[15] + "\n" +
                                                  "Application Cryptogram" + "\t" + "\t" + ": " + resp[16] + "\n" +
                                                  "CID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[17] + "\n" +
                                                  "CVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[18] + "\n" +
                                                  "TVR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[19] + "\n" +
                                                  "TSI" + "\t" + "\t" + "\t" + "\t" + ": " + resp[20] + "\n" +
                                                  "Kernal ID" + "\t" + "\t" + "\t" + "\t" + ": " + resp[21] + "\n" +
                                                  "PAR" + "\t" + "\t" + "\t" + "\t" + ": " + resp[22] + "\n" +
                                                  "Suffix" + "\t" + "\t" + "\t" + "\t" + ": " + resp[23] + "\n" +
                                                  "Card Entry Mode" + "\t" + "\t" + "\t" + ": " + resp[24] + "\n" +
                                                  "Merchant Category Code" + "\t" + "\t" + ": " + resp[25] + "\n" +
                                                  "Terminal Transaction Type" + "\t" + "\t" + ": " + resp[26] + "\n" +
                                                  "Scheme Label" + "\t" + "\t" + "\t" + ": " + resp[27] + "\n" +
                                                  "Product Info" + "\t" + "\t" + "\t" + ": " + resp[28] + "\n" +
                                                  "Application Version" + "\t" + "\t" + ": " + resp[29] + "\n" +
                                                  "Disclaimer" + "\t" + "\t" + "\t" + ": " + resp[30] + "\n" +
                                                  "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[31] + "\n" +
                                                  "Merchant Address " + "\t" + "\t" + ": " + resp[32] + "\n" +
                                                  "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[33] + "\n" +
                                                  "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[34] + "\n" +
                                                  "ECR Transaction Reference Number" + "\t" + ": " + resp[35] + "\n" +
                                                  "Signature" + "\t" + "\t" + "\t" + ": " + resp[36] + "\n";
                            if (resp[3] == "APPROVED" || resp[3] == "DECLINED")
                            {
                                string pathToHTMLFile = @"printReceipt\\Bill Pyment(Customer_copy).html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);
                                decimal Amount = Convert.ToDecimal(resp[5]) / 100;
                                Amount = Math.Round(Amount, 2);
                                string ExpiryDate = resp[9];
                                if (ExpiryDate != "")
                                {
                                    ExpiryDate = ExpiryDate.Substring(0, 2) + "/" + ExpiryDate.Substring(2, 2);
                                }
                                string respDateTime = resp[8];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);
                                builder.Replace("arabicSAR", CheckingArabic("SAR"));
                                builder.Replace("amountSAR", numToArabicConverter(string.Format("{0:0.00##}", Amount)));
                                builder.Replace("approovalcodearabic", numToArabicConverter(resp[11]));
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("panNumber", pan);
                                builder.Replace("Buzzcode", resp[6]);
                                builder.Replace("authCode", resp[11]);
                                string arabic = CheckingArabic(resp[3]);
                                arabic = arabic.Replace("\u08F1", "");
                                builder.Replace("العملية مقبولة", arabic);
                                builder.Replace("approved", resp[3]);
                                builder.Replace("CurrentAmount", (string.Format("{0:0.00##}", Amount)));
                                builder.Replace("ExpiryDate", ExpiryDate);
                                builder.Replace("CONTACTLESS", resp[24]);
                                builder.Replace("ResponseCode", resp[2]);
                                builder.Replace("AIDaid", resp[15]);
                                builder.Replace("TVR", resp[19]);
                                builder.Replace("CVR", resp[18]);
                                builder.Replace("applicationCryptogram", resp[16]);
                                builder.Replace("CID", resp[17]);
                                builder.Replace("MID", resp[13]);
                                builder.Replace("TID", resp[12]);
                                builder.Replace("RRN", resp[10]);
                                builder.Replace("StanNo", resp[7]);
                                builder.Replace("TSI", resp[20]);
                                builder.Replace("kernalId", resp[21]);
                                builder.Replace("PAR", resp[22]);
                                builder.Replace("suffix", resp[23]);
                                builder.Replace("ApplicationVersion", resp[29]);
                                builder.Replace("SchemeLabel", resp[27]);
                                builder.Replace("MerchantCategoryCode", resp[25]);
                                builder.Replace("Merchant Name", resp[31]);
                                builder.Replace("Merchant Address", resp[32]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[33]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[34]));
                                builder.Replace("Disclaimer", resp[30]);
                                string arabicpin = CheckingArabicPin(resp[30]);
                                arabicpin = arabicpin.Replace("\u08F1", "");
                                builder.Replace("التحقق", arabicpin);
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));
                            }
                        }
                        else if (responseCommand == "21")
                        {
                            if (resp[2] == "00")
                            {
                                string pathToHTMLFile = @"printReceipt\\Detail_Report.html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);

                                string summaryToHTMLFilePosTable = @"printReceipt\\PosTableRunning.html";
                                string summaryhtmlStringPosTable = File.ReadAllText(summaryToHTMLFilePosTable);
                                StringBuilder htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);

                                string summaryToHTMLFilePosDetials = @"printReceipt\\PosTerminalDetails.html";
                                string summaryhtmlStringPosDetials = File.ReadAllText(summaryToHTMLFilePosDetials);
                                StringBuilder htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);

                                string SummaryFinalReport = "";
                                string respDateTime = resp[4];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);

                                int b = 8;
                                int totalSchemeLengthL = int.Parse(resp[8]);
                                for (int j = 1; j <= totalSchemeLengthL; j++)
                                {

                                    if (resp[b + 2] == "0")
                                    {
                                        string ReconcilationToHTMLFile = @"printReceipt\\ReconcilationTable.html";
                                        string ReconcilationNoTable = File.ReadAllText(ReconcilationToHTMLFile);
                                        StringBuilder htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                        string arabi = CheckingArabic(resp[b + 1]);
                                        arabi = arabi.Replace("\u08F1", "");

                                        htmlreconcilationNoTable.Replace("Scheme", resp[b + 1]);
                                        htmlreconcilationNoTable.Replace("قَدِيرٞ", arabi);
                                        b = b + 2;

                                        SummaryFinalReport += htmlreconcilationNoTable.ToString();
                                        htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                    }
                                    else
                                    {
                                        if (resp[b + 3] == "POS TERMINAL")
                                        {
                                            string arabi = CheckingArabic(resp[b + 1]);
                                            arabi = arabi.Replace("\u08F1", "");
                                            htmlSummaryReportPosTable.Replace("مدى", arabi);
                                            htmlSummaryReportPosTable.Replace("schemename", resp[b + 1]);
                                            htmlSummaryReportPosTable.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 4])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 5]) / 100))));
                                            htmlSummaryReportPosTable.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 6])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 7]) / 100))));
                                            htmlSummaryReportPosTable.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 8])).ToString());
                                            htmlSummaryReportPosTable.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 9]) / 100))));
                                            htmlSummaryReportPosTable.Replace("CADVCount", (Convert.ToDecimal(resp[b + 10])).ToString());
                                            htmlSummaryReportPosTable.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 11]) / 100))));
                                            htmlSummaryReportPosTable.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 12])).ToString());
                                            htmlSummaryReportPosTable.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 13]) / 100))));
                                            htmlSummaryReportPosTable.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 14])).ToString());
                                            htmlSummaryReportPosTable.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 15]) / 100))));
                                            b = b + 15;
                                            SummaryFinalReport += htmlSummaryReportPosTable.ToString();
                                            htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);
                                        }
                                        else if (resp[b + 2] == "POS TERMINAL DETAILS")
                                        {
                                            j = j - 1;
                                            htmlSummaryReportPosDetials.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 3])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 4]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 5])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 6]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 7])).ToString());
                                            htmlSummaryReportPosDetials.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 8]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("CADVCount", (Convert.ToDecimal(resp[b + 9])).ToString());
                                            htmlSummaryReportPosDetials.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 10]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 11])).ToString());
                                            htmlSummaryReportPosDetials.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 12]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 13])).ToString());
                                            htmlSummaryReportPosDetials.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 14]) / 100))));
                                            b = b + 14;
                                            SummaryFinalReport += htmlSummaryReportPosDetials.ToString();
                                            htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);
                                        }
                                    }

                                }
                                builder.Replace("PosTable", SummaryFinalReport);
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("BuzzCode", resp[6]);
                                builder.Replace("AppVersion", resp[7]);
                                builder.Replace("TerminalId", TerminalId);
                                builder.Replace("Merchant Name", resp[b + 1]);
                                builder.Replace("Merchant Address", resp[b + 2]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[b + 3]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[b + 4]));
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));

                                string printSettlmentPos =
                                                  "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                   "Scheme HOST" + "\t" + "\t" + "\t" + ": SchemeHOST \n" +
                                                   "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                   "Total Debit Count" + "\t" + "\t" + "\t" + ": TotalDebitCount \n" +
                                                   "Total Debit Amount" + "\t" + "\t" + ": TotalDebitAmount \n" +
                                                   "Total Credit Count" + "\t" + "\t" + "\t" + ": TotalCreditCount \n" +
                                                   "Total Credit Amount" + "\t" + "\t" + ": TotalCreditAmount \n" +
                                                   "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                   "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                   "C/ADV Count" + "\t" + "\t" + "\t" + ": CADVCount \n" +
                                                   "C/ADV Amount" + "\t" + "\t" + "\t" + ": CADVAmount \n" +
                                                   "Auth Count" + "\t" + "\t" + "\t" + ": AuthCount \n" +
                                                   "Auth Amount" + "\t" + "\t" + "\t" + ": AuthAmount \n" +
                                                   "Total Count" + "\t" + "\t" + "\t" + ": TotalCount \n" +
                                                   "Total Amount" + "\t" + "\t" + "\t" + ": TotalAmount \n";
                                string printSettlmentPosDetails =
                                                        "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                        "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                        "P/OFF Count" + "\t" + "\t" + "\t" + ": POFFCount \n" +
                                                        "P/OFF Amount" + "\t" + "\t" + "\t" + ": POFFAmount \n" +
                                                        "P/ON Count" + "\t" + "\t" + "\t" + ": PONCount \n" +
                                                        "P/ON Amount" + "\t" + "\t" + "\t" + ": PONAmount \n" +
                                                        "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                        "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                        "REVERSAL Count" + "\t" + "\t" + "\t" + ": REVERSALCount \n" +
                                                        "REVERSAL Amount" + "\t" + "\t" + ": REVERSALAmount \n" +
                                                        "Brand EMI Count" + "\t" + "\t" + "\t" + ": Brand EMICount \n" +
                                                        "Brand EMI Amount" + "\t" + "\t" + "\t" + ": Brand EMIAmount \n" +
                                                        "COMP Count" + "\t" + "\t" + "\t" + ": COMPCount \n" +
                                                        "COMP Amount" + "\t" + "\t" + "\t" + ": COMPAmount \n";
                                StringBuilder printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                StringBuilder printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);

                                string printFinalReport1 = "";
                                int k = 8;
                                int totalSchemeLength = int.Parse(resp[8]);
                                for (int i = 1; i <= totalSchemeLength; i++)
                                {

                                    if (resp[k + 2] == "0")
                                    {
                                        string printSettlmentNO = "Scheme Name" + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                                   "<No Transaction> \n";
                                        k = k + 2;
                                        StringBuilder printSettlment2 = new StringBuilder(printSettlmentNO);
                                        printFinalReport1 += printSettlment2.ToString();
                                    }
                                    else
                                    {
                                        if (resp[k + 3] == "POS TERMINAL")
                                        {
                                            printSettlmentPos1.Replace("SchemeName", resp[k + 1]);
                                            printSettlmentPos1.Replace("TransactionAvailableFlag", resp[k + 2]);
                                            printSettlmentPos1.Replace("SchemeHOST", resp[k + 3]);
                                            printSettlmentPos1.Replace("TotalDebitCount", resp[k + 4]);
                                            printSettlmentPos1.Replace("TotalDebitAmount", resp[k + 5]);
                                            printSettlmentPos1.Replace("TotalCreditCount", resp[k + 6]);
                                            printSettlmentPos1.Replace("TotalCreditAmount", resp[k + 7]);
                                            printSettlmentPos1.Replace("NAQDCount", resp[k + 8]);
                                            printSettlmentPos1.Replace("NAQDAmount", resp[k + 9]);
                                            printSettlmentPos1.Replace("CADVCount", resp[k + 10]);
                                            printSettlmentPos1.Replace("CADVAmount", resp[k + 11]);
                                            printSettlmentPos1.Replace("AuthCount", resp[k + 12]);
                                            printSettlmentPos1.Replace("AuthAmount", resp[k + 13]);
                                            printSettlmentPos1.Replace("TotalCount", resp[k + 14]);
                                            printSettlmentPos1.Replace("TotalAmount", resp[k + 15]);
                                            k = k + 15;
                                            printFinalReport1 += printSettlmentPos1.ToString();
                                            printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                        }
                                        else if (resp[k + 2] == "POS TERMINAL DETAILS")
                                        {
                                            i = i - 1;
                                            printSettlmentPosDetails1.Replace("TransactionAvailableFlag", resp[k + 1]);
                                            printSettlmentPosDetails1.Replace("SchemeName", resp[k + 2]);
                                            printSettlmentPosDetails1.Replace("POFFCount", resp[k + 3]);
                                            printSettlmentPosDetails1.Replace("POFFAmount", resp[k + 4]);
                                            printSettlmentPosDetails1.Replace("PONCount", resp[k + 5]);
                                            printSettlmentPosDetails1.Replace("PONAmount", resp[k + 6]);
                                            printSettlmentPosDetails1.Replace("NAQDCount", resp[k + 7]);
                                            printSettlmentPosDetails1.Replace("NAQDAmount", resp[k + 8]);
                                            printSettlmentPosDetails1.Replace("REVERSALCount", resp[k + 9]);
                                            printSettlmentPosDetails1.Replace("REVERSALAmount", resp[k + 10]);
                                            printSettlmentPosDetails1.Replace("Brand EMICount", resp[k + 11]);
                                            printSettlmentPosDetails1.Replace("Brand EMIAmount", resp[k + 12]);
                                            printSettlmentPosDetails1.Replace("COMPCount", resp[k + 13]);
                                            printSettlmentPosDetails1.Replace("COMPAmount", resp[k + 14]);
                                            k = k + 14;
                                            printFinalReport1 += printSettlmentPosDetails1.ToString();
                                            printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);
                                        }
                                    }
                                }
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                     "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                     "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Trace Number" + "\t" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                     "Application Version" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                     "Total Scheme Length" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                     printFinalReport1 +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[k + 2] + "\n" +
                                                     "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[k + 3] + "\n" +
                                                      "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[k + 4] + "\n" +
                                                      "ECR Transaction Reference Number" + "\t" + ": " + resp[k + 5] + "\n" +
                                                      "Signature" + "\t" + "\t" + "\t" + ": " + resp[k + 6] + "\n";
                            }
                            else
                            {
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                    "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                    "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "ECR Transaction Reference Number : " + resp[6] + "\n" +
                                                     "Signature" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n";
                            }
                        }
                        else if (responseCommand == "22")
                        {
                            string pathToHTMLFile = @"printReceipt\\Summary_Report.html";
                            htmlString = File.ReadAllText(pathToHTMLFile);
                            StringBuilder builder = new StringBuilder(htmlString);
                            string summaryToHTMLFile = @"printReceipt\\Summary.html";
                            string summaryhtmlString = File.ReadAllText(summaryToHTMLFile);
                            StringBuilder htmlSummaryReport = new StringBuilder(summaryhtmlString);
                            string SummaryFinalReport = "";
                            string respDateTime = resp[4];
                            string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                            string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);

                            int j = 7;
                            int transactionsLength = int.Parse(resp[6]);
                            for (int i = 1; i <= transactionsLength; i++)
                            {
                                string date = resp[j + 1];
                                date = date.Substring(0, 2) + "-" + date.Substring(2, 2) + "-" + date.Substring(4, 2);
                                string time = resp[j + 6];
                                time = time.Substring(0, 2) + ":" + time.Substring(2, 2);
                                htmlSummaryReport.Replace("transactionType", resp[j]);
                                htmlSummaryReport.Replace("transactionDate", date);
                                htmlSummaryReport.Replace("transactionRRN", resp[j + 2]);
                                htmlSummaryReport.Replace("transactionAmount", (Convert.ToDecimal(resp[j + 3])).ToString());
                                htmlSummaryReport.Replace("Claim1", resp[j + 4]);
                                htmlSummaryReport.Replace("transactionState", resp[j + 5]);
                                htmlSummaryReport.Replace("transactionTime", time);
                                htmlSummaryReport.Replace("transactionPANNumber", resp[j + 7]);
                                htmlSummaryReport.Replace("authCode", resp[j + 8]);
                                htmlSummaryReport.Replace("transactionNumber", resp[j + 9]);
                                j = j + 10;
                                SummaryFinalReport += htmlSummaryReport.ToString();
                                htmlSummaryReport = new StringBuilder(summaryhtmlString);
                            }

                            builder.Replace("no_Transaction", SummaryFinalReport);
                            builder.Replace("currentTime", currentTime);
                            builder.Replace("currentDate", currentDate);
                            builder.Replace("terminalId", TerminalId.Substring(0, 8));
                            builder.Replace("CountDB", resp[j + 1]);
                            builder.Replace("AmountDB", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[j + 2]) / 100))));
                            builder.Replace("CountCR", resp[j + 3]);
                            builder.Replace("AmountCR", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[j + 4]) / 100))));
                            builder.Replace("AmountTotal", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[j + 5]) / 100))));
                            builder.Replace("merchant name", resp[j + 6]);
                            builder.Replace("merchant address", resp[j + 7]);
                            Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));

                            string printSummaryReportString = "Transaction transactionNumberHead\n" +
                                                                "----------------------------------------------------------\n" +
                                                                "Transaction Type" + "\t" + "\t" + "\t" + ": TransactionType1  \n" +
                                                                "Date" + "\t" + "\t" + "\t" + "\t" + ": Date1 \n" +
                                                                "RRN" + "\t" + "\t" + "\t" + "\t" + ": RRN1 \n" +
                                                                "Transaction Amount" + "\t" + "\t" + ": TransactionAmount1 \n" +
                                                                "Claim " + "\t" + "\t" + "\t" + "\t" + ": Claim1 \n" +
                                                                "State" + "\t" + "\t" + "\t" + "\t" + ": State1 \n" +
                                                                "Time" + "\t" + "\t" + "\t" + "\t" + ": Time1 \n" +
                                                                "PAN Number" + "\t" + "\t" + "\t" + ": PANNumber1 \n" +
                                                                "authCode" + "\t" + "\t" + "\t" + ": authCode1 \n" +
                                                                "transactionNumber" + "\t" + "\t" + ": transactionNumber1 \n" +
                                                                "----------------------------------------------------------\n";
                            StringBuilder htmlSummaryReport1 = new StringBuilder(printSummaryReportString);
                            string SummaryFinalReport1 = "";
                            int k = 7;
                            for (int a = 1; a <= transactionsLength; a++)
                            {
                                htmlSummaryReport1.Replace("transactionNumberHead", a.ToString());
                                htmlSummaryReport1.Replace("TransactionType1", resp[k]);
                                htmlSummaryReport1.Replace("Date1", resp[k + 1]);
                                htmlSummaryReport1.Replace("RRN1", resp[k + 2]);
                                htmlSummaryReport1.Replace("TransactionAmount1", resp[k + 3]);
                                htmlSummaryReport1.Replace("Claim1", resp[k + 4]);
                                htmlSummaryReport1.Replace("State1", resp[k + 5]);
                                htmlSummaryReport1.Replace("Time1", resp[k + 6]);
                                htmlSummaryReport1.Replace("PANNumber1", resp[k + 7]);
                                htmlSummaryReport1.Replace("authCode1", resp[k + 8]);
                                htmlSummaryReport1.Replace("transactionNumber1", resp[k + 9]);
                                k = k + 10;
                                SummaryFinalReport1 += htmlSummaryReport1.ToString();
                                htmlSummaryReport1 = new StringBuilder(printSummaryReportString);
                            }

                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "Transaction Requests Count" + "\t" + ": " + resp[5] + "\n" +
                                                 "Total Transactions Length" + "\t" + ": " + resp[6] + "\n" +
                                                 SummaryFinalReport1 +
                                                 "Topic" + "\t" + "\t" + "\t" + "\t" + ": " + resp[k] + "\n" +
                                                 "DB Count" + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                 "DB Amount" + "\t" + "\t" + "\t" + " : " + resp[k + 2] + "\n" +
                                                 "CR Count" + "\t" + "\t" + "\t" + ": " + resp[k + 3] + "\n" +
                                                 "CR Amount" + "\t" + "\t" + "\t" + ": " + resp[k + 4] + "\n" +
                                                 "Total Amount " + "\t" + "\t" + "\t" + ": " + resp[k + 5] + "\n" +
                                                 "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[k + 6] + "\n" +
                                                 "Merchant Address " + "\t" + "\t" + ": " + resp[k + 7] + "\n" +
                                                 "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[k + 8] + "\n" +
                                                 "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[k + 9] + "\n" +
                                                 "ECR Transaction Reference Number" + "\t" + ": " + resp[k + 10] + "\n" +
                                                 "Signature" + "\t" + "\t" + "\t" + ": " + resp[k + 11] + "\n";
                        }
                        else if (responseCommand == "23")
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                 "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                 "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                 "Previous Transaction Response" + "\t" + ": " + resp[5] + "\n" +
                                                 "Previous ECR Number" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                 "ECR Transaction Reference Number : " + resp[7] + "\n" +
                                                 "Signature Transaction" + "\t" + "\t" + ": " + resp[8] + "\n";
                        }
                        else if (responseCommand == "24")
                        {
                            BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                   "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                   "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                   "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                   "ECR Transaction Reference Number  : " + resp[5] + "\n" +
                                                   "Signature" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n";
                        }
                        else if (responseCommand == "26")
                        {
                            if (resp[2] == "00")
                            {
                                string pathToHTMLFile = @"printReceipt\\Snapshot.html";
                                htmlString = File.ReadAllText(pathToHTMLFile);
                                StringBuilder builder = new StringBuilder(htmlString);

                                string summaryToHTMLFilePosTable = @"printReceipt\\PosTableRunning.html";
                                string summaryhtmlStringPosTable = File.ReadAllText(summaryToHTMLFilePosTable);
                                StringBuilder htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);

                                string summaryToHTMLFilePosDetials = @"printReceipt\\PosTerminalDetails.html";
                                string summaryhtmlStringPosDetials = File.ReadAllText(summaryToHTMLFilePosDetials);
                                StringBuilder htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);

                                string SummaryFinalReport = "";
                                string respDateTime = resp[4];
                                string currentDate = respDateTime.Substring(2, 2) + "/" + respDateTime.Substring(0, 2) + "/" + year;
                                string currentTime = respDateTime.Substring(4, 2) + ":" + respDateTime.Substring(6, 2) + ":" + respDateTime.Substring(8, 2);

                                int b = 8;
                                int totalSchemeLengthL = int.Parse(resp[8]);
                                for (int j = 1; j <= totalSchemeLengthL; j++)
                                {

                                    if (resp[b + 2] == "0")
                                    {
                                        string ReconcilationToHTMLFile = @"printReceipt\\ReconcilationTable.html";
                                        string ReconcilationNoTable = File.ReadAllText(ReconcilationToHTMLFile);
                                        StringBuilder htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                        string arabi = CheckingArabic(resp[b + 1]);
                                        arabi = arabi.Replace("\u08F1", "");

                                        htmlreconcilationNoTable.Replace("Scheme", resp[b + 1]);
                                        htmlreconcilationNoTable.Replace("قَدِيرٞ", arabi);
                                        b = b + 2;

                                        SummaryFinalReport += htmlreconcilationNoTable.ToString();
                                        htmlreconcilationNoTable = new StringBuilder(ReconcilationNoTable);
                                    }
                                    else
                                    {
                                        if (resp[b + 3] == "POS TERMINAL")
                                        {
                                            string arabi = CheckingArabic(resp[b + 1]);
                                            arabi = arabi.Replace("\u08F1", "");
                                            htmlSummaryReportPosTable.Replace("مدى", arabi);
                                            htmlSummaryReportPosTable.Replace("schemename", resp[b + 1]);
                                            htmlSummaryReportPosTable.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 4])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 5]) / 100))));
                                            htmlSummaryReportPosTable.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 6])).ToString());
                                            htmlSummaryReportPosTable.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 7]) / 100))));
                                            htmlSummaryReportPosTable.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 8])).ToString());
                                            htmlSummaryReportPosTable.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 9]) / 100))));
                                            htmlSummaryReportPosTable.Replace("CADVCount", (Convert.ToDecimal(resp[b + 10])).ToString());
                                            htmlSummaryReportPosTable.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 11]) / 100))));
                                            htmlSummaryReportPosTable.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 12])).ToString());
                                            htmlSummaryReportPosTable.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 13]) / 100))));
                                            htmlSummaryReportPosTable.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 14])).ToString());
                                            htmlSummaryReportPosTable.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 15]) / 100))));
                                            b = b + 15;
                                            SummaryFinalReport += htmlSummaryReportPosTable.ToString();
                                            htmlSummaryReportPosTable = new StringBuilder(summaryhtmlStringPosTable);
                                        }
                                        else if (resp[b + 2] == "POS TERMINAL DETAILS")
                                        {
                                            j = j - 1;
                                            htmlSummaryReportPosDetials.Replace("totalDBCount", (Convert.ToDecimal(resp[b + 3])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalDBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 4]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("totalCBCount", (Convert.ToDecimal(resp[b + 5])).ToString());
                                            htmlSummaryReportPosDetials.Replace("totalCBAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 6]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("NAQDCount", (Convert.ToDecimal(resp[b + 7])).ToString());
                                            htmlSummaryReportPosDetials.Replace("NAQDAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 8]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("CADVCount", (Convert.ToDecimal(resp[b + 9])).ToString());
                                            htmlSummaryReportPosDetials.Replace("CADVAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 10]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("AUTHCount", (Convert.ToDecimal(resp[b + 11])).ToString());
                                            htmlSummaryReportPosDetials.Replace("AUTHAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 12]) / 100))));
                                            htmlSummaryReportPosDetials.Replace("TOTALSCount", (Convert.ToDecimal(resp[b + 13])).ToString());
                                            htmlSummaryReportPosDetials.Replace("TOTALSAmount", (string.Format("{0:0.00##}", (Convert.ToDecimal(resp[b + 14]) / 100))));
                                            b = b + 14;
                                            SummaryFinalReport += htmlSummaryReportPosDetials.ToString();
                                            htmlSummaryReportPosDetials = new StringBuilder(summaryhtmlStringPosDetials);
                                        }
                                    }

                                }
                                builder.Replace("PosTable", SummaryFinalReport);
                                builder.Replace("currentTime", currentTime);
                                builder.Replace("currentDate", currentDate);
                                builder.Replace("BuzzCode", resp[6]);
                                builder.Replace("AppVersion", resp[7]);
                                builder.Replace("TerminalId", TerminalId);
                                builder.Replace("Merchant Name", resp[b + 1]);
                                builder.Replace("Merchant Address", resp[b + 2]);
                                builder.Replace("المنزل", ConvertHexArabic(resp[b + 3]));
                                builder.Replace("بريدة", ConvertHexArabic(resp[b + 4]));
                                Dispatcher.Invoke(new Action(() => { myBrowser.NavigateToString(builder.ToString()); }));

                                string printSettlmentPos =
                                                  "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                   "Scheme HOST" + "\t" + "\t" + "\t" + ": SchemeHOST \n" +
                                                   "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                   "Total Debit Count" + "\t" + "\t" + "\t" + ": TotalDebitCount \n" +
                                                   "Total Debit Amount" + "\t" + "\t" + ": TotalDebitAmount \n" +
                                                   "Total Credit Count" + "\t" + "\t" + "\t" + ": TotalCreditCount \n" +
                                                   "Total Credit Amount" + "\t" + "\t" + ": TotalCreditAmount \n" +
                                                   "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                   "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                   "C/ADV Count" + "\t" + "\t" + "\t" + ": CADVCount \n" +
                                                   "C/ADV Amount" + "\t" + "\t" + "\t" + ": CADVAmount \n" +
                                                   "Auth Count" + "\t" + "\t" + "\t" + ": AuthCount \n" +
                                                   "Auth Amount" + "\t" + "\t" + "\t" + ": AuthAmount \n" +
                                                   "Total Count" + "\t" + "\t" + "\t" + ": TotalCount \n" +
                                                   "Total Amount" + "\t" + "\t" + "\t" + ": TotalAmount \n";
                                string printSettlmentPosDetails =
                                                        "Transaction Available Flag" + "\t" + "\t" + ": TransactionAvailableFlag \n" +
                                                        "Scheme Name" + "\t" + "\t" + "\t" + ": SchemeName \n" +
                                                        "P/OFF Count" + "\t" + "\t" + "\t" + ": POFFCount \n" +
                                                        "P/OFF Amount" + "\t" + "\t" + "\t" + ": POFFAmount \n" +
                                                        "P/ON Count" + "\t" + "\t" + "\t" + ": PONCount \n" +
                                                        "P/ON Amount" + "\t" + "\t" + "\t" + ": PONAmount \n" +
                                                        "NAQD Count" + "\t" + "\t" + "\t" + ": NAQDCount \n" +
                                                        "NAQD Amount" + "\t" + "\t" + "\t" + ": NAQDAmount \n" +
                                                        "REVERSAL Count" + "\t" + "\t" + "\t" + ": REVERSALCount \n" +
                                                        "REVERSAL Amount" + "\t" + "\t" + ": REVERSALAmount \n" +
                                                        "Brand EMI Count" + "\t" + "\t" + "\t" + ": Brand EMICount \n" +
                                                        "Brand EMI Amount" + "\t" + "\t" + "\t" + ": Brand EMIAmount \n" +
                                                        "COMP Count" + "\t" + "\t" + "\t" + ": COMPCount \n" +
                                                        "COMP Amount" + "\t" + "\t" + "\t" + ": COMPAmount \n";
                                StringBuilder printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                StringBuilder printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);

                                string printFinalReport1 = "";
                                int k = 8;
                                int totalSchemeLength = int.Parse(resp[8]);
                                for (int i = 1; i <= totalSchemeLength; i++)
                                {

                                    if (resp[k + 2] == "0")
                                    {
                                        string printSettlmentNO = "Scheme Name" + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                                   "<No Transaction> \n";
                                        k = k + 2;
                                        StringBuilder printSettlment2 = new StringBuilder(printSettlmentNO);
                                        printFinalReport1 += printSettlment2.ToString();
                                    }
                                    else
                                    {
                                        if (resp[k + 3] == "POS TERMINAL")
                                        {
                                            printSettlmentPos1.Replace("SchemeName", resp[k + 1]);
                                            printSettlmentPos1.Replace("TransactionAvailableFlag", resp[k + 2]);
                                            printSettlmentPos1.Replace("SchemeHOST", resp[k + 3]);
                                            printSettlmentPos1.Replace("TotalDebitCount", resp[k + 4]);
                                            printSettlmentPos1.Replace("TotalDebitAmount", resp[k + 5]);
                                            printSettlmentPos1.Replace("TotalCreditCount", resp[k + 6]);
                                            printSettlmentPos1.Replace("TotalCreditAmount", resp[k + 7]);
                                            printSettlmentPos1.Replace("NAQDCount", resp[k + 8]);
                                            printSettlmentPos1.Replace("NAQDAmount", resp[k + 9]);
                                            printSettlmentPos1.Replace("CADVCount", resp[k + 10]);
                                            printSettlmentPos1.Replace("CADVAmount", resp[k + 11]);
                                            printSettlmentPos1.Replace("AuthCount", resp[k + 12]);
                                            printSettlmentPos1.Replace("AuthAmount", resp[k + 13]);
                                            printSettlmentPos1.Replace("TotalCount", resp[k + 14]);
                                            printSettlmentPos1.Replace("TotalAmount", resp[k + 15]);
                                            k = k + 15;
                                            printFinalReport1 += printSettlmentPos1.ToString();
                                            printSettlmentPos1 = new StringBuilder(printSettlmentPos);
                                        }
                                        else if (resp[k + 2] == "POS TERMINAL DETAILS")
                                        {
                                            i = i - 1;
                                            printSettlmentPosDetails1.Replace("TransactionAvailableFlag", resp[k + 1]);
                                            printSettlmentPosDetails1.Replace("SchemeName", resp[k + 2]);
                                            printSettlmentPosDetails1.Replace("POFFCount", resp[k + 3]);
                                            printSettlmentPosDetails1.Replace("POFFAmount", resp[k + 4]);
                                            printSettlmentPosDetails1.Replace("PONCount", resp[k + 5]);
                                            printSettlmentPosDetails1.Replace("PONAmount", resp[k + 6]);
                                            printSettlmentPosDetails1.Replace("NAQDCount", resp[k + 7]);
                                            printSettlmentPosDetails1.Replace("NAQDAmount", resp[k + 8]);
                                            printSettlmentPosDetails1.Replace("REVERSALCount", resp[k + 9]);
                                            printSettlmentPosDetails1.Replace("REVERSALAmount", resp[k + 10]);
                                            printSettlmentPosDetails1.Replace("Brand EMICount", resp[k + 11]);
                                            printSettlmentPosDetails1.Replace("Brand EMIAmount", resp[k + 12]);
                                            printSettlmentPosDetails1.Replace("COMPCount", resp[k + 13]);
                                            printSettlmentPosDetails1.Replace("COMPAmount", resp[k + 14]);
                                            k = k + 14;
                                            printFinalReport1 += printSettlmentPosDetails1.ToString();
                                            printSettlmentPosDetails1 = new StringBuilder(printSettlmentPosDetails);
                                        }
                                    }
                                }
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                     "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                     "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Date Time Stamp" + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Trace Number" + "\t" + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "Buss Code" + "\t" + "\t" + "\t" + ": " + resp[6] + "\n" +
                                                     "Application Version" + "\t" + "\t" + ": " + resp[7] + "\n" +
                                                     "Total Scheme Length" + "\t" + "\t" + ": " + resp[8] + "\n" +
                                                     printFinalReport1 +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[k + 1] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[k + 2] + "\n" +
                                                     "Merchant Name Arabic Hex Data" + "\t" + ": " + resp[k + 3] + "\n" +
                                                      "Merchant Address Arabic Hex Data" + "\t" + ": " + resp[k + 4] + "\n" +
                                                      "ECR Transaction Reference Number" + "\t" + ": " + resp[k + 5] + "\n" +
                                                      "Signature" + "\t" + "\t" + "\t" + ": " + resp[k + 6] + "\n";
                            }
                            else
                            {
                                BUFFERRECEIVEDtext = "Transaction type" + "\t" + "\t" + "\t" + ": " + resp[1] + "\n" +
                                                    "Response Code" + "\t" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                    "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n" +
                                                     "Merchant Name " + "\t" + "\t" + "\t" + ": " + resp[4] + "\n" +
                                                     "Merchant Address " + "\t" + "\t" + ": " + resp[5] + "\n" +
                                                     "ECR Transaction Reference Number : " + resp[6] + "\n" +
                                                     "Signature" + "\t" + "\t" + "\t" + ": " + resp[7] + "\n";
                            }
                        }
                        else if (responseCommand == "50")
                        {
                            BUFFERRECEIVEDtext = "Response Code" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message " + "\t" + ": " + resp[3] + "\n";
                            // "Response Message" + "\t" + "\t" + ": " + resp[3] + "\n";

                        }
                        else
                        {
                            BUFFERRECEIVEDtext = "Response Code" + "\t" + "\t" + ": " + resp[2] + "\n" +
                                                 "Response Message " + "\t" + ": " + resp[3] + "\n";
                            (sender as BackgroundWorker).ReportProgress(1);
                        }
                    }
                    else if (responseInt == 1)
                    {
                        messageShow("Problem connecting to Terminal");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else if (responseInt == 2)
                    {
                        messageShow("Problem pack method");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else if (responseInt == 3)
                    {
                        messageShow("Problem connecting to Terminal");
                        (sender as BackgroundWorker).ReportProgress(2);
                    }
                    else if (responseInt == 4)
                    {
                        if (transactionType == "24")
                        {
                            messageShow("Terminal is busy");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                        else
                        {
                            messageShow("Timeout Please try again");
                            (sender as BackgroundWorker).ReportProgress(2);
                        }
                    }
                    (sender as BackgroundWorker).ReportProgress(1);
                }
                catch (Exception ex)
                {
                    BUFFERRECEIVEDtext = response;
                    messageShow(ex.Message);
                    (sender as BackgroundWorker).ReportProgress(2);
                }
            }
        }
        void messageShow(string message)
        {
            msgShow = message;
        }
        void worker_ProgressSend(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage.ToString() == "0")
            {
                ProgBarL.Content = "Sending...";
                ProgBarL.Visibility = Visibility.Visible;
                ProgBar.Visibility = Visibility.Visible;
                ProgBar.IsIndeterminate = true;
            }
            if (e.ProgressPercentage.ToString() == "1")
            {
                ProgBar.IsIndeterminate = false;
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                BUFFERRECEIVED.Text = BUFFERRECEIVEDtext;
                enter.IsEnabled = true;
            }
            if (e.ProgressPercentage.ToString() == "2")
            {
                ProgBar.IsIndeterminate = false;
                ProgBarL.Visibility = Visibility.Hidden;
                ProgBar.Visibility = Visibility.Hidden;
                fullScreen.IsEnabled = true;
                enter.IsEnabled = true;
                MessageBox.Show(Application.Current.MainWindow, msgShow);
            }
            if (e.ProgressPercentage.ToString() == "3")
            {
                BUFFERSEND.Text = BUFFERSENDtext;
            }
            if (e.ProgressPercentage.ToString() == "4")
            {
                if (ECRrefNum)
                {
                    chkEcrref.IsChecked = false;
                    txtEcrref.IsEnabled = false;
                }
                txtEcrref.Text = ECRREFtext;
            }
        }
        private void DisConnect(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectionService obj = new ConnectionService();


                resultDisconnect = obj.doCOMDisconnection();
                statusSerialConn = false;
                dispatcherTimer.Tick -= new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Stop();


                resultDisconnect = obj.doTCPIPDisconnection();

                if (resultDisconnect == 0)
                {
                    MessageBox.Show(Application.Current.MainWindow, "Disconnected Successfully");
                    ConnectL.Content = "Not Connected";
                    //ConnectImg.Source = null;
                    //txtIpAddress.Text = "";
                    // txtPort.Text = "";
                    BUFFERSEND.Text = "";
                    BUFFERRECEIVED.Text = "";
                    CASHBACKT.Text = "";
                    AMOUNTT.Text = "";
                    RRNT.Text = "";
                    DateT.Text = "";
                    OACT.Text = "";
                    // PORTCOM.Text = "";
                    TRANSTYPE.IsEnabled = false;
                    TRANSTYPEL.IsEnabled = false;
                    CASHBACKL.IsEnabled = false;

                    AMOUNTT.IsEnabled = false;
                    RRNL.IsEnabled = false;
                    RRNT.IsEnabled = false;
                    DateL.IsEnabled = false;
                    //DoTrans.IsEnabled = false;
                    DateT.IsEnabled = false;
                    OACL.IsEnabled = false;
                    OACT.IsEnabled = false;
                    chkEtp.IsEnabled = false;
                    rdbCom.IsEnabled = true;
                    rdbTcp.IsEnabled = true;
                    txtPort.IsEnabled = false;
                    txtIpAddress.IsEnabled = false;
                    SetSetting.IsEnabled = false;
                    chkEcrref.IsEnabled = false;
                    // SELECTTRANSTYPE.IsEnabled = false;
                    txtEcrref.IsEnabled = false;
                    CashRegisterNumberT.IsEnabled = false;
                    myBrowser.Navigate((Uri)null);
                    btnDisConnect.IsEnabled = false;
                    btnConnect.IsEnabled = true;
                    enter.IsEnabled = false;
                    TRANSTYPE.SelectedIndex = 0;
                    AdminTransaction.SelectedIndex = 0;
                    AdminTransactionL.IsEnabled = false;
                    AdminTransaction.IsEnabled = true;
                    grpTcpSetting.IsEnabled = false;
                    rdbCom.IsChecked = false;
                    rdbTcp.IsChecked = false;
                    BAUDSPEED.SelectedIndex = 5;
                    PARITY.SelectedIndex = 0;
                    DATABITS.SelectedIndex = 3;
                    STOPBITS.SelectedIndex = 0;
                    result = 1;
                    this.ProgBar.IsIndeterminate = false;
                    ProgBar.Visibility = Visibility.Hidden;
                    ProgBarL.Visibility = Visibility.Hidden;
                    txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                    txtIpAddress.Focus();
                    txtPort.Focus();
                }
                else
                {
                    MessageBox.Show(Application.Current.MainWindow, "Try Again");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.Message);
            }
        }
        private void PORTTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (txtPort.Text.Length > 4)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void AmountValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (AMOUNTT.Text.Length > 12)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void CashbackValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (CASHBACKT.Text.Length > 12)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void RRNValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (RRNT.Text.Length > 11)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void OACValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (OACT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void ECRRefValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (txtEcrref.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void prvsECRNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (prvsECRNumberT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }

        private void CashRegisterNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (CashRegisterNumberT.Text.Length > 7)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void VendorIDNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z0-9]+");

            if (VendorIDT.Text.Length > 1)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void TRSMIDNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z0-9]+");

            if (TRSMIDT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }

        private void VendorTerminaltypeNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z0-9]+");

            if (VendorTerminaltypeT.Text.Length > 1)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        //private void SAMAKeyIndexNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        //{
        //    Regex regex = new Regex("[^0-9]+");

        //    if (SAMAKeyIndexT.Text.Length > 1)
        //    {
        //        e.Handled = true;
        //    }
        //    else
        //    {
        //        e.Handled = regex.IsMatch(e.Text);
        //    }
        //}
        private void BillerIDNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (BillerIDT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void BillNumberNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            if (BillNumberT.Text.Length > 5)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = regex.IsMatch(e.Text);
            }
        }
        private void TRANSTYPESelection(object sender, SelectionChangedEventArgs e)
        {
            string type = (e.AddedItems[0] as ComboBoxItem).Content as string;
            if (type == "Register")
            {
                transaction = "Register";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                CashRegisterNumberL.Focus();
            }
            if (type == "Start Session")
            {
                transaction = "Start Session";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }
            if (type == "End Session")
            {
                transaction = "End Session";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }

            if (type == "Purchase")
            {
                transaction = "Purchase";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                countAmount = 0;
                amountDefaultVal();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();

            }
            if (type == "UPI")
            {
                transaction = "UPI";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                countAmount = 0;
                amountDefaultVal();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();

            }
            //if (type == "UPI")
            //{
            //    transaction = "UPI";
            //    AdminTransaction.SelectedIndex = 0;
            //    clearField();
            //    amountDefaultVal();
            //    cbDefaultVal();
            //    countAmount = 0;
            //    countCB = 0;
            //    CASHBACKT.IsEnabled = true;
            //    CASHBACKL.IsEnabled = true;
            //    AMOUNTL.IsEnabled = true;
            //    AMOUNTT.IsEnabled = true;
            //    RRNL.IsEnabled = false;
            //    RRNT.IsEnabled = false;
            //    DateL.IsEnabled = false;
            //    DateT.IsEnabled = false;
            //    OACL.IsEnabled = false;
            //    OACT.IsEnabled = false;
            //    chkEtp.IsEnabled = true;
            //    chkEcrref.IsEnabled = true;
            //    SetSetting.IsEnabled = false;
            //    BillerIDL.IsEnabled = false;
            //    BillerIDT.IsEnabled = false;
            //    BillNumberL.IsEnabled = false;
            //    BillNumberT.IsEnabled = false;
            //    CashRegisterNumberL.IsEnabled = false;
            //    CashRegisterNumberT.IsEnabled = false;
            //    prvsECRNumberT.IsEnabled = false;
            //    prvsECRNumberL.IsEnabled = false;
            //    myBrowser.Navigate((Uri)null);
            //    AMOUNTT.Select(AMOUNTT.Text.Length, 0);
            //    AMOUNTT.Focus();
            //}
            if (type == "Brand EMI")
            {
                transaction = "Brand EMI";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                countAmount = 0;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = true;
                DateT.IsEnabled = true;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Authorization")
            {
                transaction = "Authorization";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                countAmount = 0;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (type == "Authorization Extension")
            {
                transaction = "Authorization Extension";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                countAmount = 0;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = true;
                DateT.IsEnabled = true;
                OACL.IsEnabled = true;
                OACT.IsEnabled = true;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Authorization Void")
            {
                transaction = "Authorization Void";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                countAmount = 0;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = true;
                DateT.IsEnabled = true;
                OACL.IsEnabled = true;
                OACT.IsEnabled = true;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Purchase Advice Full")
            {
                transaction = "Purchase Advice Full";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = true;
                DateT.IsEnabled = true;
                OACL.IsEnabled = true;
                OACT.IsEnabled = true;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Purchase Advice Partial")
            {
                transaction = "Purchase Advice Partial";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = true;
                RRNT.IsEnabled = true;
                DateL.IsEnabled = true;
                DateT.IsEnabled = true;
                OACL.IsEnabled = true;
                OACT.IsEnabled = true;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Cash Advance")
            {
                transaction = "Cash Advance";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                countAmount = 0;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (type == "Reversal")
            {
                transaction = "Reversal";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                countAmount = 0;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (type == "Reconciliation")
            {
                transaction = "Reconciliation";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                countAmount = 0;
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = true;
                chkEcrref.IsEnabled = true;
                txtEcrref.IsEnabled = true;
                SetSetting.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }
            if (type == "Bill Payment")
            {
                transaction = "Bill Payment";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                amountDefaultVal();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = true;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                BillerIDL.IsEnabled = true;
                BillerIDT.IsEnabled = true;
                BillNumberL.IsEnabled = true;
                BillNumberT.IsEnabled = true;
                myBrowser.Navigate((Uri)null);
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (type == "Duplicate")
            {
                transaction = "Duplicate";
                AdminTransaction.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = true;
                myBrowser.Navigate((Uri)null);
                int refNumber = int.Parse(txtEcrref.Text) - 1;
                prvsECRNumberT.Text = refNumber.ToString("D6");

            }
            if (type == "Full Download")
            {
                transaction = "Full Download";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = true;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }
            if (type == "Partial Download")
            {
                transaction = "Partial Download";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = true;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }
            if (type == "Set Settings")
            {
                transaction = "Set Settings";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                SetSetting.IsEnabled = true;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (type == "Get Settings")
            {
                transaction = "Get Settings";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }
            if (type == "Running Total")
            {
                transaction = "Running Total";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }
            if (type == "Snapshot Total")
            {
                transaction = "Running Total";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }
            if (type == "Print Summary Report")
            {
                transaction = "Print Summary Report";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }
            if (type == "Check Status")
            {
                transaction = "Check Status";
                TRANSTYPE.SelectedIndex = 0;
                clearField();
                CASHBACKT.IsEnabled = false;
                CASHBACKL.IsEnabled = false;

                AMOUNTT.IsEnabled = false;
                RRNL.IsEnabled = false;
                RRNT.IsEnabled = false;
                DateL.IsEnabled = false;
                DateT.IsEnabled = false;
                OACL.IsEnabled = false;
                OACT.IsEnabled = false;
                chkEtp.IsEnabled = false;
                SetSetting.IsEnabled = false;
                chkEcrref.IsEnabled = false;
                txtEcrref.IsEnabled = false;
                BillerIDL.IsEnabled = false;
                BillerIDT.IsEnabled = false;
                BillNumberL.IsEnabled = false;
                BillNumberT.IsEnabled = false;
                CashRegisterNumberL.IsEnabled = false;
                CashRegisterNumberT.IsEnabled = false;
                prvsECRNumberT.IsEnabled = false;
                prvsECRNumberL.IsEnabled = false;
                myBrowser.Navigate((Uri)null);

            }
        }
        private void Clear(object sender, RoutedEventArgs e)
        {
            countAmount = 0;
            countCB = 0;
            BUFFERSEND.Text = "";
            BUFFERRECEIVED.Text = "";
            CASHBACKT.Text = "";
            AMOUNTT.Text = "";
            RRNT.Text = "";
            DateT.Text = "";
            OACT.Text = "";
            VendorIDT.Text = "";
            VendorTerminaltypeT.Text = "";
            TRSMIDT.Text = "";
            VendorKeyIndexT.Text = "";
            // SAMAKeyIndexT.Text = "";
            BillerIDT.Text = "";
            BillNumberT.Text = "";
            myBrowser.Navigate((Uri)null);
            if (transaction == "Bill Payment" || transaction == "Purchase" || transaction == "Brand EMI" || transaction == "Pre Authorization" || transaction == "Pre Auth Void" || transaction == "Purchase Advice" || transaction == "Cash Advance")
            {
                amountDefaultVal();
            }
            else if (transaction == "UPI")
            {
                amountDefaultVal();
                cbDefaultVal();
            }

        }


        private void ONE(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                AMOUNTT.Text = AMOUNTT.Text + 1;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.01";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "1";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "1";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "1";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                ////AMOUNTT.Text = AMOUNTT.Text + 1;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.01";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "1";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "1";
                }
                else
                {
                    cleanAmount = cleanAmount + "1";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 1;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 1;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 1;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 1;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 1;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 1;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 1;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 1;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 1;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 1;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 1;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 1;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 1;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 1;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 1;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }

        }

        private void AmountTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "AmountText";
        }
        private void CashbackTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "CashbackText";
        }
        private void RrnTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "RrnText";
        }
        private void OacTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "OacTextChanged";
        }
        private void IPTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "IPText";
        }
        private void PortTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "PortText";
        }
        private void ECRRefTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "ECRRefText";
        }

        private void CashRegisterNumberTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "CashRegisterNumberText";
        }
        private void VendorIDTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "VendorIDText";
        }
        private void TRSMIDTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "TRSMIDText";
        }
        private void SAMAKeyIndexTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "SAMAKeyIndexText";
        }
        private void VendorKeyIndexTectChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "VendorKeyIndexText";
        }
        private void VendorTerminaltypeTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "VendorTerminaltypeText";
        }
        private void BillPaymentAmountTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "BillPaymentAmountText";
        }
        private void BillerIDTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "BillerIDText";
        }
        private void BillNumberTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "BillNumberText";
        }
        private void prvsECRNumberTextChanged(object sender, RoutedEventArgs e)
        {
            textFocus = "prvsECRNumberText";
        }


        private void TWO(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.02";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "2";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "2";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "2";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                AMOUNTT.Text = AMOUNTT.Text + 2;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.02";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "2";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "2";
                }
                else
                {
                    cleanAmount = cleanAmount + "2";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 2;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 2;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 2;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 2;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 2;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 2;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 2;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 2;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 2;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 2;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 2;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 2;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 2;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 2;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 2;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }

        private void THREE(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.03";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "3";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "3";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "3";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                AMOUNTT.Text = AMOUNTT.Text + 3;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                //if (countCB == 1)
                //{
                //    CASHBACKT.Text = "0.03";
                //}
                //else if (countCB == 2)
                //{
                //    CASHBACKT.Text = "0." + ch[2] + "3";
                //}
                //else if (countCB == 3)
                //{
                //    CASHBACKT.Text = ch[1] + "." + ch[2] + "3";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "3";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    CASHBACKT.Text = crtFormat;
                //}
                CASHBACKT.Text = CASHBACKT.Text + 3;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 3;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 3;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 3;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 3;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 3;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 3;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 3;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 3;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 3;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 3;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 3;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 3;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 3;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 3;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void FOUR(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.04";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "4";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "4";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "4";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                AMOUNTT.Text = AMOUNTT.Text + 4;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.04";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "4";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "4";
                }
                else
                {
                    cleanAmount = cleanAmount + "4";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 4;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 4;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 4;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 4;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 4;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 4;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 4;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 4;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 4;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 4;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 4;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 4;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 4;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 4;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 4;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void FIVE(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.05";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "5";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "5";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "5";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                AMOUNTT.Text = AMOUNTT.Text + 5;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.05";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "5";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "5";
                }
                else
                {
                    cleanAmount = cleanAmount + "5";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 5;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 5;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 5;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 5;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 5;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 5;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 5;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 5;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 5;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 5;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 5;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 5;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 5;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 5;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 5;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void SIX(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.06";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "6";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "6";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "6";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                AMOUNTT.Text = AMOUNTT.Text + 6;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.06";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "6";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "6";
                }
                else
                {
                    cleanAmount = cleanAmount + "6";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 6;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 6;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 6;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 6;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 6;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 6;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 6;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 6;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 6;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 6;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 6;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 6;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 6;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 6;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 6;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void SEVEN(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.07";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "7";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "7";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "7";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                AMOUNTT.Text = AMOUNTT.Text + 7;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.07";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "7";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "7";
                }
                else
                {
                    cleanAmount = cleanAmount + "7";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 7;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 7;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 7;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 7;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 7;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 7;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 7;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 7;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 7;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 7;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 7;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 7;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 7;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 7;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 7;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void EIGHT(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.08";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "8";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "8";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "8";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                AMOUNTT.Text = AMOUNTT.Text + 8;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.08";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "8";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "8";
                }
                else
                {
                    cleanAmount = cleanAmount + "8";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 8;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 8;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 8;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 8;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 8;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 8;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 8;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 8;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 8;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 8;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 8;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 8;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 8;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 8;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 8;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void NINE(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(">>>>>>>>>>>>>>>>>>NINE" + countAmount);
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.09";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "9";
                //    Console.WriteLine(">>>>>>>>>>>>>>>>>>3" + "0." + ch[2] + "9");
                //}
                //else if (countAmount == 3)
                //{

                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "9";
                //    Console.WriteLine(">>>>>>>>>>>>>>>>>>3" + ch[1] + "." + ch[2] + "9");
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "9";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                AMOUNTT.Text = AMOUNTT.Text + 9;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.09";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "9";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "9";
                }
                else
                {
                    cleanAmount = cleanAmount + "9";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 9;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 9;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 9;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 9;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 9;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 9;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 9;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 9;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 9;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 9;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 9;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 9;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 9;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 9;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 9;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }

        private void ZERO(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length <= 12)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.00";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "0." + ch[2] + "0";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "0";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "0";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                AMOUNTT.Text = AMOUNTT.Text + 0;
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 12)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.00";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "0." + ch[2] + "0";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "0";
                }
                else
                {
                    cleanAmount = cleanAmount + "0";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + 0;
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 12)
            {
                RRNT.Text = RRNT.Text + 0;
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + 0;
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + 0;
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 6)
            {
                txtEcrref.Text = txtEcrref.Text + 0;
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 8)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + 0;
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 2)
            {
                VendorIDT.Text = VendorIDT.Text + 0;
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 6)
            {
                TRSMIDT.Text = TRSMIDT.Text + 0;
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 2)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + 0;
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 2)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + 0;
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 2)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + 0;
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 6)
            {
                BillerIDT.Text = BillerIDT.Text + 0;
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 6)
            {
                BillNumberT.Text = BillNumberT.Text + 0;
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 6)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + 0;
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 6)
            {
                OACT.Text = OACT.Text + 0;
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void DZERO(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.Length < 11)
            {
                string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countAmount++;
                string crtFormat = string.Empty;
                //if (countAmount == 1)
                //{
                //    AMOUNTT.Text = "0.00";
                //}
                //else if (countAmount == 2)
                //{
                //    AMOUNTT.Text = "00." + ch[2] + "0";
                //}
                //else if (countAmount == 3)
                //{
                //    AMOUNTT.Text = ch[1] + "." + ch[2] + "0";
                //}
                //else
                //{
                //    cleanAmount = cleanAmount + "00";
                //    for (int i = 0; i < cleanAmount.Length; i++)
                //    {
                //        if (cleanAmount.Length - 2 == i)
                //        {
                //            crtFormat = crtFormat + "." + cleanAmount[i];
                //        }
                //        else
                //        {
                //            crtFormat = crtFormat + cleanAmount[i];
                //        }
                //    }
                //    AMOUNTT.Text = crtFormat;
                //}
                //AMOUNTT.Text = AMOUNTT.Text + "00";
                AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                AMOUNTT.Focus();
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.Length < 11)
            {
                string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                char[] ch = cleanAmount.ToArray();//000
                countCB++;
                string crtFormat = string.Empty;
                if (countCB == 1)
                {
                    CASHBACKT.Text = "0.00";
                }
                else if (countCB == 2)
                {
                    CASHBACKT.Text = "00." + ch[2] + "0";
                }
                else if (countCB == 3)
                {
                    CASHBACKT.Text = ch[1] + "." + ch[2] + "0";
                }
                else
                {
                    cleanAmount = cleanAmount + "00";
                    for (int i = 0; i < cleanAmount.Length; i++)
                    {
                        if (cleanAmount.Length - 2 == i)
                        {
                            crtFormat = crtFormat + "." + cleanAmount[i];
                        }
                        else
                        {
                            crtFormat = crtFormat + cleanAmount[i];
                        }
                    }
                    CASHBACKT.Text = crtFormat;
                }
                //CASHBACKT.Text = CASHBACKT.Text + "00";
                CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                CASHBACKT.Focus();
            }
            if (textFocus.ToString() == "RrnText" && RRNT.Text.Length < 11)
            {
                RRNT.Text = RRNT.Text + "00";
                RRNT.Select(RRNT.Text.Length, 0);
                RRNT.Focus();
            }
            if (textFocus.ToString() == "IPText")
            {
                txtIpAddress.Text = txtIpAddress.Text + "00";
                txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                txtIpAddress.Focus();
            }
            if (textFocus.ToString() == "PortText")
            {
                txtPort.Text = txtPort.Text + "00";
                txtPort.Select(txtPort.Text.Length, 0);
                txtPort.Focus();
            }
            if (textFocus.ToString() == "ECRRefText" && txtEcrref.Text.Length < 5)
            {
                txtEcrref.Text = txtEcrref.Text + "00";
                txtEcrref.Select(txtEcrref.Text.Length, 0);
                txtEcrref.Focus();
            }
            if (textFocus.ToString() == "CashRegisterNumberText" && CashRegisterNumberT.Text.Length < 7)
            {
                CashRegisterNumberT.Text = CashRegisterNumberT.Text + "00";
                CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                CashRegisterNumberT.Focus();
            }
            if (textFocus.ToString() == "VendorIDText" && VendorIDT.Text.Length < 0)
            {
                VendorIDT.Text = VendorIDT.Text + "00";
                VendorIDT.Select(VendorIDT.Text.Length, 0);
                VendorIDT.Focus();
            }
            if (textFocus.ToString() == "TRSMIDText" && TRSMIDT.Text.Length < 2)
            {
                TRSMIDT.Text = TRSMIDT.Text + "00";
                TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                TRSMIDT.Focus();
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText" && SAMAKeyIndexT.Text.Length < 0)
            //{
            //    SAMAKeyIndexT.Text = SAMAKeyIndexT.Text + "00";
            //    SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //    SAMAKeyIndexT.Focus();
            //}
            if (textFocus.ToString() == "VendorKeyIndexText" && VendorKeyIndexT.Text.Length < 0)
            {
                VendorKeyIndexT.Text = VendorKeyIndexT.Text + "00";
                VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                VendorKeyIndexT.Focus();
            }
            if (textFocus.ToString() == "VendorTerminaltypeText" && VendorTerminaltypeT.Text.Length < 0)
            {
                VendorTerminaltypeT.Text = VendorTerminaltypeT.Text + "00";
                VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                VendorTerminaltypeT.Focus();
            }
            if (textFocus.ToString() == "BillerIDText" && BillerIDT.Text.Length < 5)
            {
                BillerIDT.Text = BillerIDT.Text + "00";
                BillerIDT.Select(BillerIDT.Text.Length, 0);
                BillerIDT.Focus();
            }
            if (textFocus.ToString() == "BillNumberText" && BillNumberT.Text.Length < 5)
            {
                BillNumberT.Text = BillNumberT.Text + "00";
                BillNumberT.Select(BillNumberT.Text.Length, 0);
                BillNumberT.Focus();
            }
            if (textFocus.ToString() == "prvsECRNumberText" && prvsECRNumberT.Text.Length < 5)
            {
                prvsECRNumberT.Text = prvsECRNumberT.Text + "00";
                prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                prvsECRNumberT.Focus();
            }
            if (textFocus.ToString() == "OacTextChanged" && OACT.Text.Length < 5)
            {
                OACT.Text = OACT.Text + "00";
                OACT.Select(OACT.Text.Length, 0);
                OACT.Focus();
            }
        }
        private void DELETE(object sender, RoutedEventArgs e)
        {
            if (textFocus.ToString() == "AmountText" && AMOUNTT.Text.ToString() != "0.00")
            {
                Console.WriteLine(">>>>>>>>>>>>>>>>>>DELETE bf " + countAmount);
                countAmount = countAmount - 1;
                Console.WriteLine(">>>>>>>>>>>>>>>>>>DELETE af " + countAmount);
                if (AMOUNTT.Text.Length != 0)
                {
                    string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                    cleanAmount = cleanAmount.Remove(cleanAmount.Length - 1, 1);
                    char[] ch = cleanAmount.ToArray();//000
                    string crtFormat = string.Empty;
                    Console.WriteLine(">>>>>>>>>>>>>>>>>007  " + cleanAmount);
                    if (countAmount < 1)
                    {
                        Console.WriteLine(">>>>>>>>>>>>>>>>>1  " + countAmount);
                        AMOUNTT.Text = "0.00";
                    }
                    else if (countAmount == 1)
                    {
                        Console.WriteLine(">>>>>>>>>>>>>>>>>2  " + countAmount);
                        AMOUNTT.Text = "0.0" + ch[1];
                    }
                    else if (countAmount == 2)
                    {
                        Console.WriteLine(">>>>>>>>>>>>>>>>3  " + countAmount);
                        AMOUNTT.Text = "0." + ch[0] + ch[1];
                    }
                    else if (countAmount == 3)
                    {
                        AMOUNTT.Text = ch[0] + "." + ch[1] + ch[2];
                    }
                    else
                    {
                        for (int i = 0; i < cleanAmount.Length; i++)
                        {
                            if (cleanAmount.Length - 2 == i)
                            {
                                crtFormat = crtFormat + "." + cleanAmount[i];
                            }
                            else
                            {
                                crtFormat = crtFormat + cleanAmount[i];
                            }
                            AMOUNTT.Text = crtFormat;
                        }
                        AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                        AMOUNTT.Focus();
                    }
                }
            }
            else
            {
                countAmount = 0;
            }
            if (textFocus.ToString() == "CashbackText" && CASHBACKT.Text.ToString() != "0.00")
            {
                --countCB;
                if (CASHBACKT.Text.Length != 0)
                {
                    string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                    cleanAmount = cleanAmount.Remove(cleanAmount.Length - 1, 1);
                    char[] ch = cleanAmount.ToArray();//000
                    string crtFormat = string.Empty;
                    if (countCB == 1)
                    {
                        CASHBACKT.Text = "0.00";
                    }
                    else if (countCB == 2)
                    {
                        CASHBACKT.Text = "0.0" + ch[1];
                    }
                    else if (countCB == 3)
                    {
                        CASHBACKT.Text = "0" + "." + ch[1] + ch[2];
                    }
                    else
                    {
                        for (int i = 0; i < cleanAmount.Length; i++)
                        {
                            if (cleanAmount.Length - 2 == i)
                            {
                                crtFormat = crtFormat + "." + cleanAmount[i];
                            }
                            else
                            {
                                crtFormat = crtFormat + cleanAmount[i];
                            }
                            CASHBACKT.Text = crtFormat;
                        }
                        CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                        CASHBACKT.Focus();
                    }

                }
            }
            else
            {
                countCB = 0;
            }
            if (textFocus.ToString() == "RrnText")
            {
                if (RRNT.Text.Length != 0)
                {
                    RRNT.Text = RRNT.Text.Remove(RRNT.Text.Length - 1, 1);
                    RRNT.Select(RRNT.Text.Length, 0);
                    RRNT.Focus();
                }
            }
            if (textFocus.ToString() == "IPText")
            {
                if (txtIpAddress.Text.Length != 0)
                {
                    txtIpAddress.Text = txtIpAddress.Text.Remove(txtIpAddress.Text.Length - 1, 1);
                    txtIpAddress.Select(txtIpAddress.Text.Length, 0);
                    txtIpAddress.Focus();
                }
            }
            if (textFocus.ToString() == "PortText")
            {
                if (txtPort.Text.Length != 0)
                {
                    txtPort.Text = txtPort.Text.Remove(txtPort.Text.Length - 1, 1);
                    txtPort.Select(txtPort.Text.Length, 0);
                    txtPort.Focus();
                }
            }
            if (textFocus.ToString() == "ECRRefText")
            {
                if (txtEcrref.Text.Length != 0)
                {
                    txtEcrref.Text = txtEcrref.Text.Remove(txtEcrref.Text.Length - 1, 1);
                    txtEcrref.Select(txtEcrref.Text.Length, 0);
                    txtEcrref.Focus();
                }
            }
            if (textFocus.ToString() == "CashRegisterNumberText")
            {
                if (CashRegisterNumberT.Text.Length != 0)
                {
                    CashRegisterNumberT.Text = CashRegisterNumberT.Text.Remove(CashRegisterNumberT.Text.Length - 1, 1);
                    CashRegisterNumberT.Select(CashRegisterNumberT.Text.Length, 0);
                    CashRegisterNumberT.Focus();
                }
            }
            if (textFocus.ToString() == "TRSMIDText")
            {
                if (TRSMIDT.Text.Length != 0)
                {
                    TRSMIDT.Text = TRSMIDT.Text.Remove(TRSMIDT.Text.Length - 1, 1);
                    TRSMIDT.Select(TRSMIDT.Text.Length, 0);
                    TRSMIDT.Focus();
                }
            }
            //if (textFocus.ToString() == "SAMAKeyIndexText")
            //{
            //    if (SAMAKeyIndexT.Text.Length != 0)
            //    {
            //        SAMAKeyIndexT.Text = SAMAKeyIndexT.Text.Remove(SAMAKeyIndexT.Text.Length - 1, 1);
            //        SAMAKeyIndexT.Select(SAMAKeyIndexT.Text.Length, 0);
            //        SAMAKeyIndexT.Focus();
            //    }
            //}
            if (textFocus.ToString() == "VendorKeyIndexText")
            {
                if (VendorKeyIndexT.Text.Length != 0)
                {
                    VendorKeyIndexT.Text = VendorKeyIndexT.Text.Remove(VendorKeyIndexT.Text.Length - 1, 1);
                    VendorKeyIndexT.Select(VendorKeyIndexT.Text.Length, 0);
                    VendorKeyIndexT.Focus();
                }
            }
            if (textFocus.ToString() == "VendorTerminaltypeText")
            {
                if (VendorTerminaltypeT.Text.Length != 0)
                {
                    VendorTerminaltypeT.Text = VendorTerminaltypeT.Text.Remove(VendorTerminaltypeT.Text.Length - 1, 1);
                    VendorTerminaltypeT.Select(VendorTerminaltypeT.Text.Length, 0);
                    VendorTerminaltypeT.Focus();
                }
            }
            if (textFocus.ToString() == "BillerIDText")
            {
                if (BillerIDT.Text.Length != 0)
                {
                    BillerIDT.Text = BillerIDT.Text.Remove(BillerIDT.Text.Length - 1, 1);
                    BillerIDT.Select(BillerIDT.Text.Length, 0);
                    BillerIDT.Focus();
                }
            }
            if (textFocus.ToString() == "BillNumberText")
            {
                if (BillNumberT.Text.Length != 0)
                {
                    BillNumberT.Text = BillNumberT.Text.Remove(BillNumberT.Text.Length - 1, 1);
                    BillNumberT.Select(BillNumberT.Text.Length, 0);
                    BillNumberT.Focus();
                }
            }
            if (textFocus.ToString() == "prvsECRNumberText")
            {
                if (prvsECRNumberT.Text.Length != 0)
                {
                    prvsECRNumberT.Text = prvsECRNumberT.Text.Remove(prvsECRNumberT.Text.Length - 1, 1);
                    prvsECRNumberT.Select(prvsECRNumberT.Text.Length, 0);
                    prvsECRNumberT.Focus();
                }
            }
            if (textFocus.ToString() == "OacTextChanged")
            {
                if (OACT.Text.Length != 0)
                {
                    OACT.Text = OACT.Text.Remove(OACT.Text.Length - 1, 1);
                    OACT.Select(OACT.Text.Length, 0);
                    OACT.Focus();
                }
            }
            if (textFocus.ToString() == "VendorIDText")
            {
                if (VendorIDT.Text.Length != 0)
                {
                    VendorIDT.Text = VendorIDT.Text.Remove(VendorIDT.Text.Length - 1, 1);
                    VendorIDT.Select(VendorIDT.Text.Length, 0);
                    VendorIDT.Focus();
                }
            }
        }
        private void clearField()
        {
            BUFFERSEND.Text = "";
            BUFFERRECEIVED.Text = "";
            CASHBACKT.Text = "";
            AMOUNTT.Text = "";
            RRNT.Text = "";
            DateT.Text = "";
            OACT.Text = "";
            VendorIDT.Text = "";
            VendorTerminaltypeT.Text = "";
            TRSMIDT.Text = "";
            VendorKeyIndexT.Text = "";
            // SAMAKeyIndexT.Text = "";
            BillerIDT.Text = "";
            BillNumberT.Text = "";
            prvsECRNumberT.Text = "";
        }
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            ECRrefNum = chkEcrref.IsChecked ?? false;
            if (ECRrefNum)
            {
                txtEcrref.IsEnabled = true;
            }
            else txtEcrref.IsEnabled = false;
        }
        public string ConvertStringArrayToString(string[] array)
        {
            // Concatenate all the elements into a StringBuilder.
            StringBuilder builder = new StringBuilder();
            foreach (string value in array)
            {
                builder.Append(value);
                builder.Append(';');

            }
            return builder.ToString();
        }

        public string ConvertHexArabic(string hexString)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string hs = hexString.Substring(i, 2);
                sb.Append(Convert.ToString(Convert.ToChar(Int32.Parse(hexString.Substring(i, 2), System.Globalization.NumberStyles.HexNumber))));
            }
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding iso = Encoding.GetEncoding("ISO-8859-6");
            Encoding utf16 = Encoding.Unicode;
            string msg = iso.GetString(utf16.GetBytes(sb.ToString()));

            return msg;
        }
        public string CheckingArabic(string command)
        {
            string arabic = "";
            switch (command)
            {
                case "mada":
                    arabic = "مدى";
                    break;
                case "ELECTRON":
                    arabic = "فيزا";
                    break;
                case "MAESTRO":
                    arabic = "مايسترو";
                    break;
                case "AMEX":
                    arabic = "امريكان اكسبرس";
                    break;
                case "AMERICAN EXPRESS":
                    arabic = "امريكان اكسبرس";
                    break;
                case "MASTER":
                    arabic = "ماستر كارد";
                    break;
                case "MASTERCARD":
                    arabic = "ماستر كارد";
                    break;
                case "VISA":
                    arabic = "فيزا";
                    break;
                case "GCCNET":
                    arabic = "الشبكة الخليجية";
                    break;
                case "JCB":
                    arabic = "ج س ب";
                    break;
                case "DISCOVER":
                    arabic = "ديسكفر";
                    break;
                case "SAR":
                    arabic = "ريال";
                    break;
                case "MADA":
                    arabic = "مدى";
                    break;
                case "APPROVED":
                    arabic = "مقبولة";
                    break;
                case "DECLINED":
                    arabic = "مستلمة";
                    break;
                case "ACCEPTED":
                    arabic = "مستلمة";
                    break;
                case "NOT ACCEPTED":
                    arabic = "غير مستلمة";
                    break;
                default:
                    Console.WriteLine("Invalid transactionType");
                    arabic = "";
                    break;
            }
            return arabic;
        }

        public string CheckingArabicPin(string command)
        {
            string arabic = "";
            switch (command)
            {
                case "CARDHOLDER VERIFIED BY SIGNATURE":
                    arabic = "تم التحقق بتوقيع العميل";
                    break;
                case "CARDHOLDER PIN VERIFIED":
                    arabic = "تم التحقق من الرقم السري للعميل";
                    break;
                case "DEVICE OWNER IDENTITY VERIFIED":
                    arabic = "تم التحقق من هوية حامل الجهاز";
                    break;
                case "NO VERIFICATION REQUIRED":
                    arabic = "لا يتطلب التحقق";
                    break;
                case "INCORRECT PIN":
                    arabic = "ارقم التعريف الشخصي غير صحيح";
                    break;
                default:
                    Console.WriteLine("Invalid transactionType");
                    arabic = "";
                    break;
            }
            return arabic;
        }
        public static string numToArabicConverter(string num)
        {
            string arabic = string.Empty;
            char[] numChar = num.ToCharArray();
            char[] arabicChar = { '۰', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩' };
            for (int i = 0; i < numChar.Length; i++)
            {
                char temp = numChar[i];
                string temp1 = temp.ToString();
                if (temp1 == ".")
                {
                    arabic += ".";
                }
                for (int j = 0; j < arabicChar.Length; j++)
                {
                    string tempj = j.ToString();
                    if (tempj == temp1)
                    {
                        arabic += arabicChar[j];
                    }
                }
            }
            return arabic;
        }

        private void amountDefaultVal()
        {
            AMOUNTT.Text = "0.00";
        }
        private void cbDefaultVal()
        {
            CASHBACKT.Text = "0.00";
        }

        private void AMOUNTT_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                bool checkNum = true;
                //try
                //{
                string numCheck = e.Key.ToString();//Num
                string numCheckKey = numCheck.Substring(0, 1);
                if (numCheckKey == "D")
                {
                    //string numCheck = e.Key.ToString();//Num
                    numCheck = "Num";
                }
                else
                {
                    //string numCheck = e.Key.ToString();//Num
                    numCheck = numCheck.Substring(0, 3);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Please enter number");
                //checkNum = false;
            }

        }

        private void CASHBACKT_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                bool checkNum = true;
                string numCheck = e.Key.ToString();//Num
                string numCheckKey = numCheck.Substring(0, 1);
                if (numCheckKey == "D")
                {
                    numCheck = "Num";
                }
                else
                {
                    numCheck = numCheck.Substring(0, 3);
                }
                if (checkNum)
                {
                    string cashbackAmmt = CASHBACKT.Text.ToString();
                    if (cashbackAmmt == "0.00")
                    {
                        countCB = 0;
                    }
                    string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                    char[] ch = cleanAmount.ToArray();//000
                    countCB++;
                    string crtFormat = string.Empty;
                    if (countCB < 13)
                    {
                        if (countCB == 1)
                        {
                            CASHBACKT.Text = "0.0";
                        }
                        else if (countCB == 2)
                        {
                            CASHBACKT.Text = "0." + ch[2];
                        }
                        else if (countCB == 3)
                        {
                            CASHBACKT.Text = ch[1] + "." + ch[2];
                        }
                        else
                        {

                            for (int i = 0; i < cleanAmount.Length; i++)
                            {
                                if (cleanAmount.Length - 1 == i)
                                {
                                    crtFormat = crtFormat + "." + cleanAmount[i];
                                }
                                else
                                {
                                    crtFormat = crtFormat + cleanAmount[i];
                                }
                            }
                            CASHBACKT.Text = crtFormat;
                        }
                    }
                    else
                    {
                        CASHBACKT.Text = cashbackAmmt;
                        countCB--;
                    }

                    CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                    CASHBACKT.Focus();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Please enter number");
                //checkNum = false;
            }
        }
        private void AMOUNTT_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                string backKey = e.Key.ToString();
                if (backKey == "Back")
                {
                    string cleanAmount = AMOUNTT.Text.Replace(".", string.Empty);
                    char[] ch = cleanAmount.ToArray();//000
                    countAmount--;
                    string crtFormat = string.Empty;
                    if (countAmount < 1)
                    {
                        AMOUNTT.Text = "0." + "000";
                        countAmount = 1;
                    }
                    else if (countAmount == 1)
                    {
                        AMOUNTT.Text = "0." + "0" + ch[1] + "0";
                    }
                    else if (countAmount == 2)//
                    {
                        AMOUNTT.Text = "0." + ch[0] + ch[1] + "0";
                    }
                    else
                    {
                        //cleanAmount = cleanAmount + "2";
                        for (int i = 0; i < cleanAmount.Length; i++)
                        {
                            if (cleanAmount.Length - 3 == i)
                            {
                                crtFormat = crtFormat + "." + cleanAmount[i];
                            }
                            else
                            {
                                crtFormat = crtFormat + cleanAmount[i];
                            }
                        }
                        AMOUNTT.Text = crtFormat;
                    }
                    AMOUNTT.Select(AMOUNTT.Text.Length, 0);
                    AMOUNTT.Focus();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Amount field empty");
                //checkNum = false;
            }
        }

        private void CASHBACKT_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                string backKey = e.Key.ToString();
                if (backKey == "Back")
                {
                    string cleanAmount = CASHBACKT.Text.Replace(".", string.Empty);
                    char[] ch = cleanAmount.ToArray();//000
                    countCB--;
                    string crtFormat = string.Empty;
                    if (countCB < 1)
                    {
                        CASHBACKT.Text = "0." + "000";
                        countCB = 1;
                    }
                    else if (countCB == 1)
                    {
                        CASHBACKT.Text = "0." + "0" + ch[1] + "0";
                    }
                    else if (countCB == 2)//
                    {
                        CASHBACKT.Text = "0." + ch[0] + ch[1] + "0";
                    }
                    else
                    {
                        //cleanAmount = cleanAmount + "2";
                        for (int i = 0; i < cleanAmount.Length; i++)
                        {
                            if (cleanAmount.Length - 3 == i)
                            {
                                crtFormat = crtFormat + "." + cleanAmount[i];
                            }
                            else
                            {
                                crtFormat = crtFormat + cleanAmount[i];
                            }
                        }
                        CASHBACKT.Text = crtFormat;
                    }
                    CASHBACKT.Select(CASHBACKT.Text.Length, 0);
                    CASHBACKT.Focus();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Cashback field empty");
                //checkNum = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ScanOnlineDevices();
        }
        string ip = "";
        string aport = "";
        string selectCom = "";
        string intvalue = "";
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ((Button)sender).DataContext;
            ComboBoxItem selectedComboBoxItem = comboBox.SelectedItem as ComboBoxItem;
            ConfigData data = new ConfigData();
             obj.getConfiguration(out data);
            if (selectedItem is DeviceList items)
            {
                if (items.connectionMode == "TCP/IP")
                {

                    serialNo.Text = items.SerialNo;
                    tcpip.Text = items.deviceIp;
                    tcpport.Text = items.devicePort;
                    tcpdeviceID = items.deviceId;
                    data.tcpIpDeviceId = items.deviceId;
                    data.comDeviceId = data.comDeviceId;
                    obj.setConfiguration(data);
                }
                else
                {

                    comserialNo.Text = items.SerialNo;
                    comfullname.Text = items.COM;
                    comdeviceID = items.deviceId;
                    data.tcpIpDeviceId = data.tcpIpDeviceId;
                    data.comDeviceId = items.deviceId;
                    obj.setConfiguration(data);

                }
               
            }
        }

        private void ConnectComButton_Click(object sender, RoutedEventArgs e)
        {

            var selectedItem = ((Button)sender).DataContext;

            // Check if the selected item is a list
            if (selectedItem is DeviceList items)
            {
                selectCom = "";
                selectCom = items.COM;
                intvalue = "";
                foreach (char c in selectCom)
                {
                    if (char.IsDigit(c))
                    {
                        intvalue += c;
                    }
                }
                isOnlineDevice = obj.testSerialCom(int.Parse(intvalue));
                if (isOnlineDevice == true)
                {
                    btnDisConnect.IsEnabled = true;
                    enter.IsEnabled = true;
                    AMOUNTT.IsEnabled = true;
                    ConnectL.Content = "Connected";
                }
                else
                {
                    MessageBox.Show("Problem Connecting with Terminal");
                }
            }

            //var result = obj.doCOMConnection(int.Parse(IPortCom), baudRateCom, parityCom, dataBitsCom, stopBitsCom);

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DOCOMConnection();
        }

        string loglevel = string.Empty;
        bool islogAllowed;
        int noOfDayValue = 0;
        string filepath = string.Empty;
        String priority1;
        String priority2;
        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            bool isFallbackAllowed = false;
            string firstPriority = "";
            string secondPriority = "";
            string loglevelval = LogLevel.Text;
            bool result;
            int a;

            switch (loglevelval.ToLower()) // Convert input to lowercase for case-insensitivity
            {
                case "error":
                    loglevel = "1";
                    break;
                case "debug":
                    loglevel = "4";
                    break;
                case "information":
                    loglevel = "3";
                    break;
                default:
                    break;
            }
            Dispatcher.Invoke(() =>
            {
                islogAllowed = isEnabledlog.IsChecked == true;

            });

            if (isEnabledlog.IsChecked == true)
            {
                result = int.TryParse(NoDay.Text, out a);

                if (!result || int.Parse(NoDay.Text.ToString()) < 1)
                {
                    NoDay.Text ="1";
                    //MessageBox.Show("Please enter a valid Retain day value");
                    NoDay.Focus();
                    //return;
                }
            }

            if (isEnabledlog.IsChecked == false)
            {
                loglevel = "6";
                LogFile.SetLogOptions(int.Parse(loglevel), true, filepath, noOfDayValue);
            }

            string noOfRetainValue = NoDay.Text;

            Dispatcher.Invoke(() =>
            {
                isFallbackAllowed = connectivityFallbackCheckBox.IsChecked == true;
            });

            ComboBoxItem selectedComboBoxItem = comboBox.SelectedItem as ComboBoxItem;

            if (selectedComboBoxItem != null)
            {
                string selectedPriority = selectedComboBoxItem.Content.ToString();


                if (selectedPriority == "TCP/IP")
                {
                    firstPriority = "TCP/IP";
                    secondPriority = "COM";
                    TCPlbl.Text = "TCP IP";
                    COMlbl.Text = "COM";
                    TCPIPGrid.Visibility = Visibility.Visible;
                    TCPIPGrid1.Visibility = Visibility.Hidden;
                    COMGrid.Visibility = Visibility.Visible;
                    COMGrid1.Visibility = Visibility.Hidden;
                    TCPIPDeviceID.Text = tcpdeviceID;
                    TCPIPIP.Text = savetcpport;
                    TCPPORT.Text = savetcpIp;
                    TCPSerialNO.Text = savetcpserialno;
                    COMDeviceID.Text = comdeviceID;
                    COMSerialPort.Text = savecomport;
                    COMSrialNO.Text = savecomserialno;

                }
                else
                {
                    firstPriority = "COM";
                    secondPriority = "TCP/IP";
                    TCPlbl1.Text = "COM";
                    COMlbl1.Text = "TCP IP";
                    TCPIPGrid.Visibility = Visibility.Hidden;
                    TCPIPGrid1.Visibility = Visibility.Visible;
                    COMGrid.Visibility = Visibility.Hidden;
                    COMGrid1.Visibility = Visibility.Visible;
                    COMDeviceID1.Text = savecomserialno;
                    COMSerialPort1.Text = comdeviceID;
                    COMSerialNO.Text = savecomport;
                    TCPIPIP1.Text = savetcpIp;
                    TCPSrialNO1.Text = savetcpserialno;
                    TCPIPPORT.Text = savetcpport;
                    TCPDeviceID1.Text = tcpdeviceID;

                }
                string[] connectionPriorityMode = new string[] { firstPriority, secondPriority };
                ConfigData fetchData = new ConfigData();
                int configData =  obj.getConfiguration(out fetchData);
                if (configData == 0)
                {
                    fetchData.isAppidle = true;
                    fetchData.commPortNumber = fetchData.commPortNumber;

                    fetchData.communicationPriorityList = connectionPriorityMode;
                    for (int i = 0; i < fetchData.communicationPriorityList.Length; i++)
                    {
                        fetchData.connectionMode = fetchData.communicationPriorityList[i];
                        break;
                    }
                    fetchData.comfullName = comfullname.Text;
                    fetchData.comserialNumber = comserialNo.Text;
                    
                    fetchData.comDeviceId = fetchData.comDeviceId;
                    if (fetchData.comDeviceId == "")
                    {
                        fetchData.comDeviceId = comdeviceID;
                    }
                    fetchData.tcpIpaddress = tcpip.Text;
                    fetchData.tcpIpPort = tcpport.Text;
                    fetchData.tcpIpSerialNumber = serialNo.Text;
                    
                    fetchData.tcpIpDeviceId = fetchData.tcpIpDeviceId;
                    if(fetchData.tcpIpDeviceId=="")
                    {
                        fetchData.tcpIpDeviceId = tcpdeviceID;
                    }
                    fetchData.tcpIp = fetchData.tcpIp;
                    fetchData.tcpPort = fetchData.tcpPort;
                    fetchData.retainDay = NoDay.Text;
                    fetchData.communicationPriorityList = fetchData.communicationPriorityList;
                    fetchData.isConnectivityFallBackAllowed = isFallbackAllowed;
                    fetchData.retry = retrivalCount.Text;
                    fetchData.retainDay = noOfRetainValue;
                    string Timeout = ConnectionTimeOut.Text;
                    if (int.Parse(Timeout) >= 29)
                    {
                        ConnectionTimeOut.Text = Timeout;
                        fetchData.connectionTimeOut = Timeout;
                    }
                    else
                    {
                        MessageBox.Show("Please Select time out more than or equal to 30 sec", "3000");
                        ConnectionTimeOut.Text = "30";
                        ConnectionTimeOut.Focus();
                        return;
                    }
                    fetchData.LogPath = filepath;
                    fetchData.loglevel = loglevel;
                    fetchData.logtype = LogLevel.Text;
                }
                if (noOfRetainValue != "")
                {
                    noOfDayValue = int.Parse(noOfRetainValue);
                    if (islogAllowed)
                    {
                        if (Filepath.Text != "")
                        {
                            filepath = Filepath.Text;
                            if (!Directory.Exists(Path.GetDirectoryName(filepath)))
                            {
                                MessageBox.Show("Invalid file path. Please enter a valid file path.");
                                return;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(filepath) && Directory.Exists(Path.GetDirectoryName(filepath)))
                        {
                            LogFile.SetLogOptions(int.Parse(loglevel), islogAllowed, filepath, noOfDayValue);
                        }
                    }
                }
                MessageBox.Show("Settings saved successfully.", "Confirmation", MessageBoxButton.OK);
                clearTcpIPGrid();
                saveUsersettings();
                obj.setConfiguration(fetchData);
                Log.Information(JsonConvert.SerializeObject(fetchData));
                showData();

            }
        }
        private void clearTcpIPGrid()
        {
            TcpIpList.ItemsSource = null;
            TcpIpList.Visibility = Visibility.Hidden;
        }

        private void saveUsersettings()
        {
            float result;
            try
            {

                Properties.Settings.Default.ConnectionFallback = connectivityFallbackCheckBox.IsChecked.ToString();
                Properties.Settings.Default.LogOption = isEnabledlog.IsChecked.ToString();
                Properties.Settings.Default.Path = Filepath.Text;
                Properties.Settings.Default.LogLevel = LogLevel.Text;
                Properties.Settings.Default.Priority = comboBox.Text;
                Properties.Settings.Default.ConnectionTimeOut = ConnectionTimeOut.Text;
                Properties.Settings.Default.RetryTime = retrivalCount.Text;

                if ((string.IsNullOrEmpty(Properties.Settings.Default.RetainDays)) || 
                    (!string.IsNullOrEmpty(Properties.Settings.Default.RetainDays) && 
                    (float.Parse(Properties.Settings.Default.RetainDays) != float.Parse(NoDay.Text.ToString()))))
                {
                    Properties.Settings.Default.retentionDate = DateTime.Now.AddDays(float.Parse(NoDay.Text) - 1).ToString();
                }

                Properties.Settings.Default.RetainDays = NoDay.Text;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {

            }
        }

        private void Priority1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string ComboBoxitem = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;
            if (ComboBoxitem == "TCP/IP")
            {
                Scan_online_device.Visibility = Visibility.Visible;
            }
            else if (ComboBoxitem == "COM")
            {

                Scan_online_device.Visibility = Visibility.Visible;
            }

        }

        private void Priority2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string ComboBoxitem = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;

            if (ComboBoxitem == "TCP/IP")
            {
                Scan_online_device.Visibility = Visibility.Visible;

            }
            else if (ComboBoxitem == "COM")
            {

                Scan_online_device.Visibility = Visibility.Visible;
            }

        }

        private void ProcessBtn(object sender, RoutedEventArgs e)
        {
            if (SELECTTRANSTYPE.Text.ToString() != "" && SELECTTRANSTYPE.Text != "------------- Transaction type ---------")
            {
                Btnprocess.IsEnabled = false;
                transTypeSelectedPos = SELECTTRANSTYPE.Text.ToString();
                OnlineTrans();
            }
            else
            {

                MessageBox.Show("Please select a Valid transaction type", "2003");
            }
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            LogLevel.IsEnabled = true;
            NoDay.IsEnabled = true;
            broswing.IsEnabled = true;
            Filepath.IsEnabled = false;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrEmpty(comfullname.Text))
            {
                MessageBox.Show("COM is In Empty.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            intvalue = string.Empty;
            selectCom = comfullname.Text;
            foreach (char c in selectCom)
            {
                if (char.IsDigit(c))
                {
                    intvalue += c;
                }
            }
            if (intvalue.Length > 3)
            {
                int number = int.Parse(intvalue);

                int lastDigit = number % 10;
                intvalue = lastDigit.ToString();
            }
            isOnlineDevice = obj.testSerialCom(int.Parse(intvalue));
            if (isOnlineDevice == true)
            {

                savecomport = comfullname.Text;
                savecomserialno = comserialNo.Text;
                enter.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                ConnectL.Content = "Connected";
                Log.Information("Connected COM Port: " + "COM" + intvalue);
                Log.Information("COM connected Successfully","2000");
                MessageBox.Show("COM connected Successfully","2000");
            }
            else
            {
                MessageBox.Show("COM connected failed", "2001");
                Log.Information("COM connection failed","2001");
                Btnprocess.IsEnabled = true;
            }
        }
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tcpip.Text))
            {
                MessageBox.Show("IP is In Empty.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(tcpport.Text))
            {
                MessageBox.Show("Port is Empty", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (tcpport.Text.Length >= 6)
            {
                MessageBox.Show("Port number cannot be more than 6 characters long", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            isOnlineDevice = obj.testTCP(tcpip.Text, int.Parse(tcpport.Text));
            if (isOnlineDevice)
            {
                enter.IsEnabled = true;
                AMOUNTT.IsEnabled = true;
                ConnectL.Content = "Connected";
                savedeviceid = "123456";
                savetcpIp = tcpip.Text;
                savetcpport = tcpport.Text;
                savetcpserialno = serialNo.Text;
                Log.Information(PosLibConstant.TCPIPCONNECTION_SUCCESS, "1000");
                Log.Information("Connected ip:" + tcpip.Text);
                Log.Information("Connected port:" + tcpport.Text);
                MessageBox.Show("TCP IP Connection Successful","1000");
            }
            else
            {
                MessageBox.Show("tcp/ip connection fail","1001");
                Log.Information("tcp/ip connection fail", "1001");
                Log.Information("is tcp/ip connected:" + "False");
            }

        }
        
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select a folder"
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string selectedFolderPath = dialog.FileName;
                Filepath.Text = selectedFolderPath;
            }
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            EditBtn.Visibility = Visibility.Hidden;
            CashierID.IsEnabled = true;
            CashireName.IsEnabled = true;
            savebtn.Visibility = Visibility.Visible;
        }
        private void savebtn_Click(object sender, RoutedEventArgs e)
        {
            savebtn.Visibility = Visibility.Hidden;
            CashierID.IsEnabled = false;
            CashireName.IsEnabled = false;
           
            if (CashierID.Text != "")
            {
                EditBtn.Visibility = Visibility.Visible;
                fetchData.CashierID = CashierID.Text;
                fetchData.CashierName = CashireName.Text;
                obj.setConfiguration(fetchData);
            }
            else
            {
                MessageBox.Show("Please select CashierID");
                CashierID.IsEnabled = true;
                CashireName.IsEnabled = true;
                savebtn.Visibility = Visibility.Visible;
            }  
        }

        private void CashierID_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void tcpport_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                e.Handled = true;
            }
            else
            {
                string newText = tcpport.Text + e.Text;
                if (newText.Length > 4)
                {
                    e.Handled = true;
                    MessageBox.Show("Port number should be 4 digits.");
                }
            }
        }

        private void isEnabledlog_Unchecked_1(object sender, RoutedEventArgs e)
        {
            broswing.IsEnabled = false;
            NoDay.IsEnabled = false;
            LogLevel.IsEnabled = false;
            Filepath.IsEnabled = false;
        }
    }
}
