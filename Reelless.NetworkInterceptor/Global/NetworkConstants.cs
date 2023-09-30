using System.Net;
using System.Net.NetworkInformation;
using SharpPcap.LibPcap;

namespace Reelless.NetworkInterceptor.Global
{
    public static partial class Constants
    {
        public static LibPcapLiveDevice captureDevice = LibPcapLiveDeviceList.Instance[0];
        public static IPAddress myIpAddress = captureDevice.Addresses[1].Addr.ipAddress;
        public static PhysicalAddress myMacAddress = captureDevice.MacAddress;
    }
}