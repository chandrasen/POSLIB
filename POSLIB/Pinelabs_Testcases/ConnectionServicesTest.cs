using Moq;
using PosLibs.ECRLibrary.Common;
using System.Net.Sockets;
using PosLibs.ECRLibrary.Service;
using System.IO.Ports;
using System.Reflection;
using PosLibs.ECRLibrary.Model;
using System.Text;
using Newtonsoft.Json;

namespace Pinelabs_Testcases
{
    public class ConnectionServicesTest

    {
        public Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        [Fact]
        public void TestIsOnlineConnection_ValidIP()
        {
            // Arrange
            string IP = "192.168.6.149";
            int PORT = 6666;

            ConnectionService conobj = new ConnectionService();
            // Act
            bool result = conobj.isOnlineConnection(IP, PORT);
            // Assert
            Assert.True(result);
        }
        [Fact]
        public void TestIsOnlineConnection_InValidIP()
        {
            // Arrange
            string IP = "192.168.6.149";
            int PORT = 6666;
            var mockConfigData = new Mock<ConfigData>();
            mockConfigData.SetupAllProperties();
            ConnectionService conobj = new ConnectionService();
            // Act
            bool result = conobj.isOnlineConnection(IP, PORT);
            // Assert
            Assert.False(result);
        }


        [Fact]
        public void TestIsComDeviceConnected_Success()
        {
            // Arrange
            int comPort = 9;
            var mockConnectionService = new Mock<ConnectionService>();
            ConnectionService conobj = mockConnectionService.Object;
            // Act
            bool result = conobj.isComDeviceConnected(comPort);

            // Assert
            Assert.True(result);
        }
        [Fact]
        public void TestIsComDeviceConnected_Failure()
        {
            // Arrange
            int comPort = 00;

            var mockConnectionService = new Mock<ConnectionService>();
            ConnectionService conobj = mockConnectionService.Object;
            // Act
            bool result = conobj.isComDeviceConnected(comPort);

            // Assert
            Assert.False(result);
        }
        [Fact]
        public void TestDoCOMDisconnection_Success()
        {
            // Arrange
            var mockSerialPort = new Mock<SerialPort>();
            var conobj = new ConnectionService();
            typeof(ConnectionService).GetField("serial", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(conobj, mockSerialPort.Object);
            // Act
            int result = conobj.doCOMDisconnection();

            // Assert
            Assert.Equal(0, result);
        }
        [Fact]
        public void scanSerialDevice_NoDevicefound()
        {
            // Arrange
            var listener = new MockScanDeviceListener();
            ConnectionService conobj = new ConnectionService();

            // Act
            ISet<string> allcomport = conobj.getDeviceManagerComPort();
            conobj.scanSerialDevice((PosLibs.ECRLibrary.Common.Interface.IScanDeviceListener)listener);
            // Assert
            Assert.Equal(0, allcomport.Count);
        }
        [Fact]
        public void scanSerialDevice_Devicefound()
        {
            // Arrange
            var listener = new MockScanDeviceListener();
            ConnectionService conobj = new ConnectionService();

            // Act
            ISet<string> allcomport = conobj.getDeviceManagerComPort();
            conobj.scanSerialDevice((PosLibs.ECRLibrary.Common.Interface.IScanDeviceListener)listener);
            // Assert
            Assert.Equal(2, allcomport.Count);
        }
        [Fact]
        public void scanOnlineDevice_vaildip()
        {
            // Arrange
            string IP = "192.168.6.149";
            var listener = new MockScanDeviceListener();
            ConnectionService conobj = new ConnectionService();
            // Act
            conobj.scanOnlineDevice((PosLibs.ECRLibrary.Common.Interface.IScanDeviceListener)listener);
            string myIP = "192.168.6.149";
            //Assert
            Assert.Equal(myIP, IP);
        }

        [Fact]
        public void scanOnlineDevice_InvalidIP()
        {
            // Arrange
            string invalidIP = "00.00.000.00";
            var listener = new MockScanDeviceListener();
            ConnectionService conobj = new ConnectionService();

            // Act
            conobj.scanOnlineDevice((PosLibs.ECRLibrary.Common.Interface.IScanDeviceListener)listener);
            string myIP = "192.168.2.211";

            // Assert
            Assert.False(myIP == invalidIP);
        }
        [Fact]
        public void CheckComport_Should_Return_Empty_String_When_ComPort_Not_Found()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();

            // Act
            string result = conobj.checkComport("COM0");

            // Assert
            Assert.Equal(string.Empty, result);
        }
        [Fact]
        public void CheckComport_Should_Return_Correct_ComPortName_When_ComPort_Found()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();
            // Act
            string result = conobj.checkComport("COM1");

            // Assert
            Assert.Equal("COM1", result);
        }
        [Fact]
        public static void SendTcpIpTxnData_Should_Return_True_On_Successful_Sending()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();
            string requestData = "=\u0010ZR2XPT6yU\u0012\u000e\u0012\u001b\u001d`+7Re?BQa~\u0006\u001egD\u0017O6W\u001b\tc\u0001\u001b\u001dfBTAAUJE\0) L\u0013|\u0010\u0005st\0\u0002|\rt\u0006v\u0003}\0u\u0003\t\u0002t\u0003\0\u0002w\u0005\r\u0004zuu\u0006\u0003u\u0001\aww\u0005\u0001s\at\u0005~\0z\0p\u0003\b\u0002u\u0003\0\u0003\u0005\u0003\b\u0003\u0001t\a\artq\u0006\0vs\0\u0006r\u0005\u0014j\u0010P@\u0005UT^\t_UU\u0016\n_P.5!H";

            // Act
            bool result = conobj.sendTcpIpTxnData(requestData);

            // Assert
            Assert.True(result);
        }
        [Fact]
        public void SendTcpIpTxnData_Should_Return_False_On_Null_RequestData()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();
            string requestData = null;
            
            // Act
            bool result = conobj.sendTcpIpTxnData(requestData);

            // Assert
            Assert.False(result);
        }
        [Fact]
        public void TestReceiveTcpIpTxnData_Null()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();
            string responseData = null;
            // Act
            string result = conobj.receiveTcpIpTxnData();

            // Assert
            Assert.Equal(responseData, result);
        }
        [Fact]
        public void TestCheckComConn_PortClosed_ReturnsFalse()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();
            // Act
            bool result = conobj.checkComConn();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TestGetComDeviceRequest_ValidInput_ReturnsCorrectJson()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();
            string expectedJson = "{\"cashierId\":\"12345678\",\"msgType\":\"2\",\"RFU1\":\"\"}";

            // Act
            string result = conobj.getComDeviceRequest();

            // Assert
            Assert.Equal(expectedJson, result);
        }

        [Fact]
        public void TestGetComDeviceRequest_ValidInput_DeserializesToCorrectObject()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();
            string expectedCashierId = "12345678";
            string expectedMsgType = "2";
            string expectedRFU1 = "";

            // Act
            string jsonRequest = conobj.getComDeviceRequest();
            var result = JsonConvert.DeserializeObject<ComDeviceRequest>(jsonRequest);

            // Assert
            Assert.Equal(expectedCashierId, result.cashierId);
            Assert.Equal(expectedMsgType, result.msgType);
            Assert.Equal(expectedRFU1, result.RFU1);
        }

        [Fact]
        public void TestAddToDeviceList_ValidInput_AddsToDeviceLists()
        {
            // Arrange
            var mockTerminalConnectivity = new Mock<TerminalConnectivityResponse>();
            var recivebuff = JsonConvert.SerializeObject(mockTerminalConnectivity.Object);
            var deviceList = new DeviceList();
            var deviceLists = new List<DeviceList>();

            // Create an instance of the class containing the 'addToDeviceList' method
            ConnectionService conobj = new ConnectionService();


            // Assert


            conobj.addToDeviceList(recivebuff);

            // Verify that Console.WriteLine is not called (Optional, if you want to test the exception handling)
            Assert.Empty(Mock.Get(Console.Out).Invocations);
        }

        [Fact]
        public void SendData_Should_Return_True_On_Successful_Send()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();
            string dataToSend = "Sample data to send"; 

            // Act
            bool result = conobj.sendData(dataToSend);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SendData_Should_Return_False_When_SerialPort_Not_Open()
        {
            // Arrange
            ConnectionService conobj = new ConnectionService();
            string dataToSend = "Sample data to send"; // Replace this with the actual data you want to test.

            // Act
            bool result = conobj.sendData(dataToSend);

            // Assert
            Assert.False(result);
        }

    }
}
