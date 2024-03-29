﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public class ConfigData
    {
        public ConfigData()
        {
            tcpIp = string.Empty;
            connectionMode = string.Empty;
            CashierID = string.Empty;
            CashierName = string.Empty;
            comfullName = string.Empty;
            comserialNumber = string.Empty;
            tcpIpaddress = string.Empty;
            retry = string.Empty;
            connectionTimeOut = string.Empty;
            retainDay=string.Empty;
            tcpIpPort = string.Empty;
            logtype = string.Empty;
            tcpIpDeviceId = string.Empty;
            tcpIpSerialNumber = string.Empty;
            comDeviceId = string.Empty;
            LogPath = string.Empty;
            loglevel = string.Empty;
            deviceHealthCheckSerialNumber = string.Empty;
        }
        public int commPortNumber { get; set; }
        public string tcpIp { get; set; }
        public int tcpPort { get; set; }
        public string connectionMode { get; set; }
        public string[] communicationPriorityList { get; set; } = Array.Empty<string>();
        public bool isConnectivityFallBackAllowed { get; set; }
        public string CashierID { get; set; }
        public string CashierName { get; set; }
        public string retry { get; set; }
        public string connectionTimeOut { get; set; }
        public string comfullName { get; set; }
        public string comserialNumber { get; set; }
        public string tcpIpaddress { get; set; }
        public string tcpIpPort { get; set; }
        public string tcpIpDeviceId { get; set; }
        public string tcpIpSerialNumber { get; set; }
        public string comDeviceId { get; set; }
        public string LogPath { get; set; }
        public string loglevel { get; set; }
        public bool isAppidle { get; set; }
        public string retainDay { get; set; }
        public string logtype { get; set; }
        public string deviceHealthCheckSerialNumber { get; set; }
        public bool isDeviceNumberMatch { get; set; }

    }
}
