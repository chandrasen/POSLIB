using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PosLibs.ECRLibrary.Model;

namespace PosLibs.ECRLibrary.Common
{
    public interface IScanDeviceListener
    {
        void onSuccess(List<DeviceList> list);
        void onFailure(string errorMsg, int errorCode);

    }
}
