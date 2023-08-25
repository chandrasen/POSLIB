using System.IO.Ports;

namespace Pinelabs_Testcases
{
    public class MockSerialPort: SerialPort
    {
        public bool IsOpen { get; set; }

        public void Close()
        {
            this.IsOpen = false;
        }
    }
}