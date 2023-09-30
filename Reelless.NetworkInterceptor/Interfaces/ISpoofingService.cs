using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Reelless.NetworkInterceptor.Interfaces
{
    public interface ISpoofingService
    {
        ///<summary>
        /// Initiates spoofing attack on a target list, creating a thread for each target
        ///</summary>
        /// <param name="targetList"> Targets to become their MITM (Man In The Middle)</param>
        /// <param name="gatewayIpAddress"> Ip address of the router</param>
        /// <param name="gatewayMacAddress"> Mac address of the router</param>
        public void StartSpoof(Dictionary<IPAddress, PhysicalAddress> targetList, IPAddress gatewayIpAddress, PhysicalAddress gatewayMacAddress);
    }
}