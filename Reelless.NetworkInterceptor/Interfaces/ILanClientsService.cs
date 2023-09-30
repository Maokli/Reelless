using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Reelless.NetworkInterceptor.Interfaces
{
    public interface ILanClientsService
    {
        /// <summary>
        /// Captures the conntected clients from the list
        /// </summary>
        public void CaptureLanClients();

        /// <summary>
        /// Captures the conntected clients from the list.
        /// Does not display the list on the console
        /// </summary>
        public void CaptureLanClientsSilent();

        /// <summary>
        /// Cleans the recorded clients list and updates it
        /// </summary>
        public void RefreshClientsList();

        ///<summary>
        /// Returns the clients list
        ///</summary>
        public Dictionary<IPAddress, PhysicalAddress> GetLanClientsList();
    }
}