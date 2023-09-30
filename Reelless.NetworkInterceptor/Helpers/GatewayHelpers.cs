using System.Net;
using System.Net.NetworkInformation;
using Reelless.NetworkInterceptor.Global;

namespace Reelless.NetworkInterceptor.Helpers
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
        public static PhysicalAddress GetGatewayMAC(Dictionary<IPAddress, PhysicalAddress> clientlist, int retryCount = 0)
        {
            int maxRetryCount = 5;
            try
            {
                // We try to get the gateway mac
                IPAddress gatewayip = GetGatewayIP();
                return clientlist[gatewayip];
            }
            catch (System.Exception)
            {
                // We are allowed to fail only "maxRetryCount" times
                if(retryCount > maxRetryCount)
                {
                    throw new Exception("Could not locate gateway through scan!");
                }

                retryCount++;
                // If we fail we re-get the client list
                LanClientsList.RefreshClientsList();
                
                return GetGatewayMAC(LanClientsList.GetLanClientsList(), retryCount);
            }
        }
    }
}