using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using sandbox.Global;
using sandbox.Helpers;
using SharpPcap;

namespace sandbox.Service
{
    public class RoutingService
    {
        private Dictionary<IPAddress, PhysicalAddress> _victimList;

        public RoutingService(Dictionary<IPAddress, PhysicalAddress> victimList)
        {
            _victimList = victimList;   
        }
        
        public void StartCapture()
        {
            Constants.captureDevice.Filter = "tcp";
            Constants.captureDevice.OnPacketArrival += FilterPackets;
            Console.WriteLine("-- Listening on {0}, hit 'Enter' to stop...",
    Constants.captureDevice.Description);
            Console.WriteLine(Constants.captureDevice.MacAddress.ToString());
            Constants.captureDevice.Capture();
        }

        private void FilterPackets(object s, PacketCapture e)
        {
            Packet packet = e.GetPacket().GetPacket();
            EthernetPacket ethernetPacket = (EthernetPacket)packet.Extract<EthernetPacket>();
            IPv4Packet ipv4Packet = (IPv4Packet)ethernetPacket.Extract<IPv4Packet>();
            // if(ethernetPacket.DestinationHardwareAddress.Equals(Constants.captureDevice.MacAddress) && !ipv4Packet.DestinationAddress.Equals(Constants.myIpAddress))
            // {
            //     Console.WriteLine("----------Corrupt Packet----------");
            //     Console.WriteLine(ethernetPacket);
            //     // EthernetPacket healthyPacket = CorrectEthernetPacket(ethernetPacket, ipv4Packet);
            //     // Console.WriteLine("----------Healthy Packet----------");
            //     // Console.WriteLine(healthyPacket);
            //     // Constants.captureDevice.SendPacket(healthyPacket);
            // }
                
            //     if(ipv4Packet != null && ipv4Packet.Protocol == ProtocolType.Icmp && ipv4Packet.SourceAddress != Constants.myIpAddress)
            //         Console.WriteLine(packet);
            // if(ipv4Packet != null && ipv4Packet.Protocol == ProtocolType.Icmp && ipv4Packet.SourceAddress != Constants.myIpAddress)
            // {

            if(IsPacketForRouter(ethernetPacket))
            {
                Console.WriteLine("----------Corrupt Packet For Router Recieved----------");
                Console.WriteLine(ethernetPacket);
                EthernetPacket healthyPacket = CorrectEthernetPacketForRouter(ethernetPacket);
                Console.WriteLine("----------Sent Healthy Packet To Router----------");
                Console.WriteLine(healthyPacket);
                    if(healthyPacket.Bytes.Count() < 1500) 
                Constants.captureDevice.SendPacket(healthyPacket);
            }
            // if(IsPacketForVictim(ethernetPacket))
            // {
            //     Console.WriteLine("----------Corrupt Packet For Victim Recieved----------");
            //     Console.WriteLine(ethernetPacket);
            //     EthernetPacket healthyPacket = CorrectEthernetPacketForVictim(ethernetPacket);
            //     Console.WriteLine("----------Sent Healthy Packet To Victim----------");
            //     Console.WriteLine(healthyPacket);
            //     if(healthyPacket.Bytes.Count() < 1500) 
            //         Constants.captureDevice.SendPacket(healthyPacket);
            // }
            //}
        }
        private EthernetPacket CorrectEthernetPacketForRouter(EthernetPacket corruptEthernetPacket)
        {
            PhysicalAddress correctMacAddress = GatewayHelpers.GetGatewayMAC(_victimList);

            // We correct the mac address and return the healthy packet
            EthernetPacket healthyPacket = corruptEthernetPacket;
            healthyPacket.DestinationHardwareAddress = correctMacAddress;
            healthyPacket.SourceHardwareAddress = Constants.myMacAddress;
            return healthyPacket;
        }

        private EthernetPacket CorrectEthernetPacketForVictim(EthernetPacket corruptEthernetPacket)
        {

            IPv4Packet ipv4Packet = (IPv4Packet)corruptEthernetPacket.Extract<IPv4Packet>();

            PhysicalAddress correctMacAddress = _victimList[ipv4Packet.DestinationAddress];

            // We correct the mac address and return the healthy packet
            EthernetPacket healthyPacket = corruptEthernetPacket;
            healthyPacket.DestinationHardwareAddress = correctMacAddress;
            healthyPacket.SourceHardwareAddress = Constants.myMacAddress;
            return healthyPacket;
        }

        /// <summary>
        /// tells if a packet is supposed to be sent to the router
        /// </summary>
        /// <param name="ethernetPacket"> the packet to check whether coming from the router or not </param>
        /// <return> True if the packet is supposed to be sent to the router, False if not. </return>
        private bool IsPacketForRouter(EthernetPacket ethernetPacket)
        {
            return ethernetPacket.DestinationHardwareAddress.Equals(Constants.myMacAddress) 
                && !ethernetPacket.SourceHardwareAddress.Equals(GatewayHelpers.GetGatewayMAC(_victimList));
        }

        private bool IsPacketForVictim(EthernetPacket ethernetPacket)
        {
            IPv4Packet ipv4Packet = (IPv4Packet)ethernetPacket.Extract<IPv4Packet>();

            return ethernetPacket.DestinationHardwareAddress.Equals(Constants.myMacAddress) 
                && !ipv4Packet.DestinationAddress.Equals(Constants.myIpAddress);
        }

        private bool packetISICMP(Packet p)
        {
            bool isICMP = true;
            string hex = p.PrintHex();
            for(int i = 61; i <= 69; i++)
            {
                if(!hex.Contains(i.ToString()))
                    isICMP = false;
            }

            return isICMP;
        }
    }
}