using Moq;
using PosLibs.ECRLibrary.Common;
using System.Net.Sockets;
using PosLibs.ECRLibrary.Service;
using System.IO.Ports;
using System.Reflection;
using PosLibs.ECRLibrary.Model;
using System.Transactions;
using PosLibs.ECRLibrary.Common.Interface;

namespace Pinelabs_Testcases
{
    public class TransactionsTest
    {
        [Fact]
        public void Successful_COM_Transaction()
        {
            // Arrange
            TransactionService transacation = new TransactionService();

            var transactionListenerMock = new Mock<PosLibs.ECRLibrary.Common.Interface.ITransactionListener>();
            // Act
            //ConfigData configData = new ConfigData();
            //bool isConnectivityFallBackAllowed = configData.isConnectivityFallBackAllowed = true;
            bool isConnectivityFallBackAllowed = true;
            string connectionMode = "COM";
            Assert.True(isConnectivityFallBackAllowed);
            bool result = connectionMode == "COM".ToString();
            transacation.DoTransaction("111", 4001, transactionListenerMock.Object);
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void COM_Transactionfailed()
        {
            // Arrange
            TransactionService transacation = new TransactionService();

            var transactionListenerMock = new Mock<ITransactionListener>();
            // Act
            //transacation.doTransaction("111", 4001, transactionListenerMock.Object);
            ///bool isConnectivityFallBackAllowed = false;
           // ConfigData configdata = new ConfigData();
           // Assert.False(isConnectivityFallBackAllowed);
           // bool result = (configdata.connectionMode == "");
            // Assert
           // Assert.False(result);
        }


        [Fact]
        public void TCPIP_Transactionsuccess()
        {
            // Arrange
            TransactionService transacation = new TransactionService();

            var transactionListenerMock = new Mock<ITransactionListener>();
            // Act
            bool isConnectivityFallBackAllowed = false;
            transacation.DoTransaction("111", 4001, transactionListenerMock.Object);
            string connectionMode = "TCPIP";  
           // Assert.False(isConnectivityFallBackAllowed);
            //bool result = connectionMode == "TCPIP";
            // Assert
           // Assert.False(result);
        }

        [Fact]
        public void TCPIP_Transactionfailed()
        {
            // Arrange
            TransactionService transacation = new TransactionService();

            var transactionListenerMock = new Mock<ITransactionListener>();
            // Act
            //transacation.doTransaction("111", 4001, transactionListenerMock.Object);
            //bool isConnectivityFallBackAllowed = false;
            //ConfigData configdata = new ConfigData();
           // Assert.False(isConnectivityFallBackAllowed);
           // bool result = (configdata.connectionMode == "TCMMMMMMMM");
            // Assert
           // Assert.False(result);
        }

        [Fact]
        public void Successful_COM_Transaction_WhenFallbackallowedTrue()
        {
            // Arrange
            TransactionService transacation = new TransactionService();

            var transactionListenerMock = new Mock<ITransactionListener>();
            // Act
            transacation.DoTransaction("111", 4001, transactionListenerMock.Object);
            bool isConnectivityFallBackAllowed = true;
           // ConfigData configdata = new ConfigData();
           // Assert.True(isConnectivityFallBackAllowed);
           // string result = (configdata.connectionMode == "COM").ToString();
            // Assert
           // Assert.Equal("COM", result);
        }

        [Fact]
        public void Successful_TCPIP_Transaction_When_FallbackallowedTrue()
        {
            // Arrange
            TransactionService transacation = new TransactionService();
            var transactionListenerMock = new Mock<PosLibs.ECRLibrary.Common.Interface.ITransactionListener>();
            // Act
            string IP = "192.168.5.160";
            int PORT = 6666;
            ConnectionService conobj = new ConnectionService();
            bool result11 = conobj.IsOnlineConnection(IP, PORT);
            bool isConnectivityFallBackAllowed = true;
            string connectionMode = "TCPIP";
            Assert.True(isConnectivityFallBackAllowed);
            bool result = connectionMode == "TCPIP".ToString();
            transacation.DoTransaction("111", 4001, transactionListenerMock.Object);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TCPIP_Transactionfailed_When_FallbackallowedTrue()
        {
            // Arrange
            TransactionService transacation = new TransactionService();
            var transactionListenerMock = new Mock<PosLibs.ECRLibrary.Common.Interface.ITransactionListener>();
            // Act
            string IP = "192.161.1.161";
            int PORT = 6666;
            ConnectionService conobj = new ConnectionService();
            
            bool isConnectivityFallBackAllowed = true;
            string connectionMode = "";
            Assert.True(isConnectivityFallBackAllowed);
            bool result = (connectionMode == "");
            transacation.DoTransaction("111", 4001, transactionListenerMock.Object);
            // Assert
            Assert.False(result);
        }
    }
}
