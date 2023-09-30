using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using Reelless.NetworkInterceptor.Global;
using Reelless.NetworkInterceptor.Helpers;
using Reelless.NetworkInterceptor.Interfaces;
using SharpPcap;

namespace Reelless.NetworkInterceptor.Services
{
    public class RoutingService : IRoutingService
    {
        private Dictionary<IPAddress, PhysicalAddress> _victimList;

        public RoutingService(Dictionary<IPAddress, PhysicalAddress> victimList)
        {
            _victimList = victimList;   
        }
        
        /// <summary>
        /// Listens to the network traffic and fires an action when a packet is recieved.
        /// </summary>
        public void StartCapture()
        {
            Constants.captureDevice.Filter = "tcp";
            Constants.captureDevice.OnPacketArrival += OnPacketArrivalHandler;
            Console.WriteLine("-- Listening on {0}, hit 'Enter' to stop...",
                Constants.captureDevice.Description);
            Console.WriteLine(Constants.captureDevice.MacAddress.ToString());
            Constants.captureDevice.Capture();
        }

        /// <summary>
        /// This function will be called whenever a packet is recieved.
        /// IT should decide how to handle the packet. 
        /// it has two choices: Block the packet or correct it and send it.
        /// </summary>
        private void OnPacketArrivalHandler(object s, PacketCapture e)
        {
            Packet packet = e.GetPacket().GetPacket();
            EthernetPacket ethernetPacket = packet.Extract<EthernetPacket>();

            if(ShouldBlockPacket(ethernetPacket))
                return;
            
            CorrectPacketAndSentIt(corruptEthernetPacket: ethernetPacket);
        }

        /// <summary>
        /// Determines whether the packet should be blocked or not.
        /// </summary>
        /// <param name="ethernetPacket"> The packet to check is allowed or not.</param>
        /// <returns>True if the packet shall not pass. False if not</returns>
        private bool ShouldBlockPacket(EthernetPacket ethernetPacket)
        {
            //TODO: implement this
            // IPV4 packet contains more info about the intentions of the packet.
            IPv4Packet ipv4Packet = ethernetPacket.Extract<IPv4Packet>();

            return false;
        }

        /// <summary>
        /// Corrects the corrupt ethernet packet and sends it.
        /// </summary>
        private void CorrectPacketAndSentIt(EthernetPacket corruptEthernetPacket)
        {
            if(IsPacketForRouter(corruptEthernetPacket))
            {
                EthernetPacket healthyPacket = CorrectEthernetPacketForRouter(corruptEthernetPacket);
                
                if(healthyPacket.Bytes.Count() < 1500) 
                    Constants.captureDevice.SendPacket(healthyPacket);
            }

            if(IsPacketForVictim(corruptEthernetPacket))
            {
                EthernetPacket healthyPacket = CorrectEthernetPacketForVictim(corruptEthernetPacket);
                if(healthyPacket.Bytes.Count() < 1500) 
                    Constants.captureDevice.SendPacket(healthyPacket);
            }
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

        /// <summary>
        /// tells if a packet is supposed to be sent to the victim
        /// </summary>
        /// <return> True if the packet is supposed to be sent to the victim, False if not. </return>

        private bool IsPacketForVictim(EthernetPacket ethernetPacket)
        {
            IPv4Packet ipv4Packet = (IPv4Packet)ethernetPacket.Extract<IPv4Packet>();

            return ethernetPacket.DestinationHardwareAddress.Equals(Constants.myMacAddress) 
                && !ipv4Packet.DestinationAddress.Equals(Constants.myIpAddress);
        }
    }
}