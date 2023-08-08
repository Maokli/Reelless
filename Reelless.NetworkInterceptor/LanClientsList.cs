using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using SharpPcap;
using sandbox.Global;

namespace sandbox
{
    public static class LanClientsList
    {
        private static Dictionary<IPAddress, PhysicalAddress> _clientlist = new Dictionary<IPAddress, PhysicalAddress>();

        /// <summary>
        /// Captures the conntected clients from the list
        /// </summary>
        public static void CaptureLanClients()
        {
            
            Constants.captureDevice.Open(DeviceModes.Promiscuous, 1000);
            Console.WriteLine("Sending Sockets To All the devices");
            SendArpPacketsToAll();
            Thread.Sleep(1000);
            Console.WriteLine("Capturing Devices' Responses");
            CaptureFloatingArpPackets();
            Thread.Sleep(5000);
            Constants.captureDevice.Close();
        }

        ///<summary>
        /// Returns the clients list
        ///</summary>
        public static Dictionary<IPAddress, PhysicalAddress> GetLanClientsList()
        {
            return _clientlist;
        }

        /// <summary>
        /// Beatifully displays the connected clients info
        /// </summary>
        public static void DisplayLanClientsList()
        {
            Console.WriteLine("   IP ADDRESS     |     MAC ADDRESS    ");
            Console.WriteLine("---------------------------------------");
            foreach (var client in _clientlist)
            {
                Console.WriteLine("{0} | {1}", client.Key, client.Value);
            }
        }

        /// <summary>
        /// Sends ARP packets to all possible clients in order to recieve them and identify connected devices
        /// </summary>
        private static void SendArpPacketsToAll()
        {
            //send arp packets to all possible ip adresses
            try
            {
                for(int ipIndex = 1; ipIndex <= Constants.MAX_IP_RANGE; ipIndex++)
                {
                    //if(ipIndex == 101) continue; //belaarbi taa zby

                    ArpPacket arprequestpacket = new ArpPacket(ArpOperation.Request, PhysicalAddress.Parse("00-00-00-00-00-00"), IPAddress.Parse(GetRootIp(Constants.myIpAddress) + ipIndex), Constants.captureDevice.MacAddress, Constants.myIpAddress);
                    EthernetPacket ethernetpacket = new EthernetPacket(Constants.captureDevice.MacAddress, PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF"), EthernetType.Arp);
                    ethernetpacket.PayloadPacket = arprequestpacket;
                    Constants.captureDevice.SendPacket(ethernetpacket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("---------Cannot send arp packets---------");
                Console.WriteLine(ex.Message);
            }
        }

        ///<summary>
        /// Captures the floating responses to the previously sent arp requests and
        /// uses the sender's mac and ip adress to populate the lan clients list
        ///</summary>
        private static void CaptureFloatingArpPackets()
        {
            Constants.captureDevice.Filter = "arp";
            long scanduration = 5000;
            //capture floating arp packets
            new Thread(
                () => {
                    try
                    {
                        PacketCapture packetCapture;
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        Console.WriteLine();
                        Console.Write("Scanning: 0%   ");
                        while ((Constants.captureDevice.GetNextPacket(out packetCapture)) == GetPacketStatus.PacketRead && stopwatch.ElapsedMilliseconds <= scanduration)
                        {
                            RawCapture rawcapture = packetCapture.GetPacket();
                            Packet packet = Packet.ParsePacket(rawcapture.LinkLayerType, rawcapture.Data);
                            ArpPacket arppacket = (ArpPacket)packet.Extract<ArpPacket>();
                            if (!_clientlist.ContainsKey(arppacket.SenderProtocolAddress) && arppacket.SenderProtocolAddress.ToString() != "0.0.0.0" && areCompatibleIPs(arppacket.SenderProtocolAddress, Constants.myIpAddress))
                            {
                                _clientlist.Add(arppacket.SenderProtocolAddress, arppacket.SenderHardwareAddress);
                            }
                            int percentageprogress = (int)((float)stopwatch.ElapsedMilliseconds / scanduration * 100);
                            Console.Write("\rScanning: {0}%   ", percentageprogress);
                        }
                        stopwatch.Stop();
                        Console.WriteLine();
                        Console.WriteLine("Finished scanning: {0} devices found!", _clientlist.Count);
                        DisplayLanClientsList();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        Console.WriteLine("---------Cannot capture arp packets---------");
                        Console.WriteLine(ex.Message);
                    }
                }
            ).Start();

        }

        /// <summary>
        /// Converts say 192.168.1.4 to 192.168.1.
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <returns></returns>
        private static string GetRootIp(IPAddress ipaddress)
        {
            string ipaddressstring = ipaddress.ToString();
            return ipaddressstring.Substring(0, ipaddressstring.LastIndexOf(".") + 1);
        }

        /// <summary>
        /// Checks if both IPAddresses have the same root ip
        /// </summary>
        /// <param name="ip1"></param>
        /// <param name="ip2"></param>
        /// <returns></returns>
        private static bool areCompatibleIPs(IPAddress ip1, IPAddress ip2)
        {
            return (GetRootIp(ip1) == GetRootIp(ip2));
        }
    }
}