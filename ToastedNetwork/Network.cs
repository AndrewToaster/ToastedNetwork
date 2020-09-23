using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToastedNetwork
{
    /// <summary>
    /// Global registry of default variable used to make a net - work (pun intended)
    /// </summary>
    public static class Network
    {
        /// <summary>
        /// The name of the application to figure out what connections are valid
        /// </summary>
        public static string AppIdentifier { get; set; }

        /// <summary>
        /// The default wait time used for when waiting for a response
        /// </summary>
        public static int RequestTimeout { get; set; }

        /// <summary>
        /// The default timeout used for when connecting to a remote host
        /// </summary>
        public static int ConnectTimeout { get; set; }

        /// <summary>
        /// The default communication channel used for <see cref="NetPeer"/> communication
        /// </summary>
        public static int DefaultChannel { get; set; }

        /// <summary>
        /// The default type of delivery of <see cref="PacketModel.IPacket"/>s to a connection
        /// </summary>
        public static NetDeliveryMethod DeliveryMethod { get; set; }

        /// <summary>
        /// Represent the allowed message types that will carry a packet
        /// </summary>
        public static NetIncomingMessageType AllowedPacketTypes { get; set; }

        /// <summary>
        /// This boolean says if when we try to autheticate, with default method, on server should we allow it or no
        /// </summary>
        public static bool ShouldAuthWhenDefault { get; set; }

        private static bool b_Init;

        static Network()
        {
            AppIdentifier = "PLEASE_USE_A_DIFFERENT_VALUE_THIS_WILL_CAUSE_PROBLEMS_WITH_FOREIGN_CONNECTIONS";
            RequestTimeout = 5000;
            ConnectTimeout = 15000;
            DefaultChannel = 0;
            ShouldAuthWhenDefault = true;
            AllowedPacketTypes = NetIncomingMessageType.ConnectionApproval | NetIncomingMessageType.UnconnectedData | NetIncomingMessageType.Data;
            DeliveryMethod = NetDeliveryMethod.ReliableOrdered;

            b_Init = false;
        }

        /// <summary>
        /// Initializes the network variables
        /// </summary>
        /// <param name="identifier">The application identifier</param>
        /// <returns><see cref="bool"/> Indicating if the <see cref="Network"/> was initialized by 
        /// <see cref="Initilize(string)"/>. Returns <c>false</c> if it was already initialized</returns>
        public static bool Initilize(string identifier)
        {
            if (!b_Init)
            {
                AppIdentifier = identifier;
                b_Init = true;
            }

            return !b_Init;
        }
    }
}
