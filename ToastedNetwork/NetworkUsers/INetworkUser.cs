using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ToastedNetwork.PacketModel;

namespace ToastedNetwork.NetworkUsers
{
    /// <summary>
    /// Base class for all network users
    /// </summary>
    public interface INetworkUser
    {
        /// <summary>
        /// Unique Identifier
        /// </summary>
        long Identifier { get; }

        /// <summary>
        /// Method used for sending packets to an <see cref="IPEndPoint"/>
        /// </summary>
        /// <param name="packet">The <see cref="IPacket"/> to send to the <paramref name="endPoint"/></param>
        /// <param name="endPoint">The <see cref="IPEndPoint"/> destination for the <paramref name="packet"/></param>
        void SendPacket(IPacket packet, IPEndPoint endPoint);
    }
}
