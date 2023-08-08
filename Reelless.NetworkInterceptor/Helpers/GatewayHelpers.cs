using System.Net;
using System.Net.NetworkInformation;
using sandbox.Global;

namespace sandbox.Helpers
{
    public static class GatewayHelpers
    {
        public static IPAddress GetGatewayIP()
        {
            IPAddress? retval = null;
            string friendlyName = Constants.captureDevice.Interface.FriendlyName;
            if (friendlyName != "")
            {
                foreach (var networkinterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkinterface.Name == friendlyName)
                    {
                        foreach (var gateway in networkinterface.GetIPProperties().GatewayAddresses)
                        {
                            if (gateway.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) //filter ipv4 gateway ip address
                                retval = gateway.Address;
                        }
                    }
                }
            }

            if(retval == null)
                throw new Exception("cannot locate gateway");
            
            return retval;
        }
        public static PhysicalAddress GetGatewayMAC(Dictionary<IPAddress, PhysicalAddress> clientlist)
        {
            IPAddress gatewayip = GetGatewayIP();
            return clientlist[gatewayip];
        }
    }
}