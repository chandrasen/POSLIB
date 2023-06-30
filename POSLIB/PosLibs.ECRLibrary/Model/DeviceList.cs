using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Model
{
    public class DeviceList
    {
        public DeviceList() { }
        public Boolean isBtDevice { get; set; }
        public string deviceId { get; set; }
        public string cashierId { get; set; }
        public string MerchantName { get; set; }
        public string SerialNo { get; set; }
        public string deviceIp { get; set; }
        public string devicePort { get; set; }
        public string btDeviceName { get; set; }

        public string connectionMode { get; set; }
        public int msgType { get; set; }
        public string COM { get; set; }
    }
}
