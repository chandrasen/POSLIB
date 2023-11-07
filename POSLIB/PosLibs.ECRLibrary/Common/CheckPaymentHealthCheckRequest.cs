using Newtonsoft.Json;
using PosLibs.ECRLibrary.Common.Interface;
using PosLibs.ECRLibrary.Model;
using PosLibs.ECRLibrary.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public class CheckPaymentHealthCheckRequest
    {
       readonly ConnectionService _connectionService = new ConnectionService();
        

        public string CheckTcpIpHealthRequest()
        {
            ConfigData config;
            _connectionService.getConfiguration(out config);
            if (config == null)
            {
                return " ";
            }
            TCPIPHealthCheck healthCheck = new TCPIPHealthCheck()
            {
               
                cashierid = config.CashierID,
                 msgType=CommonConst.msgType1_7,
                 pType=CommonConst.pType,
                
            };
            string json = JsonConvert.SerializeObject(healthCheck);
            return json;
        }
        public string CheckCompHealthRequest()
        {
            ConfigData config;
            _connectionService.getConfiguration(out config);
            if (config == null)
            {
                return " ";
            }
            ComHealthCheckRequest healthCheck = new ComHealthCheckRequest()
            {
                cashierId = config.CashierID,
                msgType = CommonConst.msgType1_7,
                pType = CommonConst.pType,

            };
            string json = JsonConvert.SerializeObject(healthCheck);
            return json;
        }
    }
}
