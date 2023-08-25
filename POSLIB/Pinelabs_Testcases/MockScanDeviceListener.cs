using PosLibs.ECRLibrary.Common;
using PosLibs.ECRLibrary.Common.Interface;
using PosLibs.ECRLibrary.Model;

namespace Pinelabs_Testcases
{
    public class MockScanDeviceListener : IScanDeviceListener
    { 
        public int onFailureCalls;
        public int onSuccessCalls;
        public List<DeviceList> deviceLists;

        public void onFailure(string errorMessage, int errorCode)
        {
            this.onFailureCalls++;
        }
        public void onSuccess(List<DeviceList> list)
        {
            this.deviceLists = deviceLists;
            this.onSuccessCalls++;
        }
    }
}