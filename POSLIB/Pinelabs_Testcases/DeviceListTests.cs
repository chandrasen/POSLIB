using PosLibs.ECRLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pinelabs_Testcases
{
    public class DeviceListTests
    {
        public static string savetcpIp = string.Empty;

        [Fact]
        public void TestDeviceList_DeviceId()
        {
            // Arrange
            string expectedDeviceId = "123456";
            DeviceList device = new DeviceList();

            // Act
            device.deviceId = expectedDeviceId;
            string actualDeviceId = device.deviceId;

            // Assert
            Assert.Equal(expectedDeviceId, actualDeviceId);
        }

        [Fact]
        public void TestDeviceList_CashierId()
        {
            // Arrange
            string expectedCashierId = "123456";
            DeviceList device = new DeviceList();

            // Act
            device.cashierId = expectedCashierId;
            string actualCashierId = device.cashierId;

            // Assert
            Assert.Equal(expectedCashierId, actualCashierId);
        }

        [Fact]
        public void TestDeviceList_MerchantName()
        {
            // Arrange
            string expectedMerchantName = "Manjunath";
            DeviceList device = new DeviceList();

            // Act
            device.MerchantName = expectedMerchantName;
            string actualMerchantName = device.MerchantName;

            // Assert
            Assert.Equal(expectedMerchantName, actualMerchantName);
        }

        [Fact]
        public void TestDeviceList_SerialNo()
        {
            // Arrange
            string expectedSerialNo = "12345678";
            DeviceList device = new DeviceList();

            // Act
            device.SerialNo = expectedSerialNo;
            string actualSerialNo = device.SerialNo;

            // Assert
            Assert.Equal(expectedSerialNo, actualSerialNo);
        }

        [Fact]
        public void TestDeviceList_DeviceIp()
        {
            // Arrange
            string expectedDeviceIp = "192.168.5.84";
            DeviceList device = new DeviceList();

            // Act
            device.deviceIp = expectedDeviceIp;
            string actualDeviceIp = device.deviceIp;

            // Assert
            Assert.Equal(expectedDeviceIp, actualDeviceIp);
        }

        [Fact]
        public void TestDeviceList_DevicePort()
        {
            // Arrange
            string expectedDevicePort = "6666";
            DeviceList device = new DeviceList();

            // Act
            device.devicePort = expectedDevicePort;
            string actualDevicePort = device.devicePort;

            // Assert
            Assert.Equal(expectedDevicePort, actualDevicePort);
        }
        [Fact]
        public void TestDeviceList_ConnectionMode()
        {
            // Arrange
            string expectedConnectionMode = "TCP/IP";
            DeviceList device = new DeviceList();

            // Act
            device.connectionMode = expectedConnectionMode;
            string actualConnectionMode = device.connectionMode;

            // Assert
            Assert.Equal(expectedConnectionMode, actualConnectionMode);
        }

        [Fact]
        public void TestDeviceList_MsgType()
        {
            // Arrange
            int expectedMsgType = 2;
            DeviceList device = new DeviceList();

            // Act
            device.msgType = expectedMsgType;
            int actualMsgType = device.msgType;

            // Assert
            Assert.Equal(expectedMsgType, actualMsgType);
        }

        [Fact]
        public void TestDeviceList_COM()
        {
            // Arrange
            string expectedCOM = "COM3";
            DeviceList device = new DeviceList();

            // Act
            device.COM = expectedCOM;
            string actualCOM = device.COM;

            // Assert
            Assert.Equal(expectedCOM, actualCOM);
        }
    }
}
