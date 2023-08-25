using PosLibs.ECRLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pinelabs_Testcases
{
    public class ComDeviceRequestTests
    {
        [Fact]
        public void TestComDeviceRequest_CashierId()
        {
            // Arrange
            string expectedCashierId = "12345678";
            ComDeviceRequest request = new ComDeviceRequest();

            // Act
            request.cashierId = expectedCashierId;
            string actualCashierId = request.cashierId;

            // Assert
            Assert.Equal(expectedCashierId, actualCashierId);
        }

        [Fact]
        public void TestComDeviceRequest_MsgType()
        {
            // Arrange
            string expectedMsgType = "2";
            ComDeviceRequest request = new ComDeviceRequest();

            // Act
            request.msgType = expectedMsgType;
            string actualMsgType = request.msgType;

            // Assert
            Assert.Equal(expectedMsgType, actualMsgType);
        }

        [Fact]
        public void TestComDeviceRequest_RFU1()
        {
            // Arrange
            string expectedRFU1 = "";
            ComDeviceRequest request = new ComDeviceRequest();

            // Act
            request.RFU1 = expectedRFU1;
            string actualRFU1 = request.RFU1;

            // Assert
            Assert.Equal(expectedRFU1, actualRFU1);
        }

        [Fact]
        public void TestComDeviceRequest_NullableProperties()
        {
            // Arrange
            ComDeviceRequest request = new ComDeviceRequest();

            // Act & Assert
            Assert.Null(request.cashierId);
            Assert.Null(request.msgType);
            Assert.Null(request.RFU1);
        }
    }
}
