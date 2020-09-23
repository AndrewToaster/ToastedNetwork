using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToastedNetwork.NetworkUsers;
using ToastedNetwork.PacketModel;
using ToastedNetwork.Packets;

namespace ToastedNetwork
{
    /// <summary>
    /// A static class for all custom delegates
    /// </summary>
    public static class Delegates
    {
        /// <summary>
        /// Delegate used when a client connects to a server
        /// </summary>
        /// <param name="id">The clients unique identifier</param>
        public delegate void ClientConnected(NetConnection client);

        /// <summary>
        /// Delegate used when client connects to remote server
        /// </summary>
        /// <param name="server">The remote connection</param>
        public delegate void ConnectedToServer(NetConnection server);

        /// <summary>
        /// Called when a packet is received, called befory any other packet handler
        /// </summary>
        /// <param name="packet">Packet send</param>
        public delegate void PacketReceived(IPacket packet);

        /// <summary>
        /// Delegate used for when a new packet comes, called after <see cref="PacketReceived"/>
        /// </summary>
        /// <param name="packet">The DataPacket received</param>
        public delegate void DataPacketReceived(DataPacket packet);

        /// <summary>
        /// Delegate used for when a new packet comes, called after <see cref="PacketReceived"/>
        /// </summary>
        /// <param name="packet">The DataPacket received</param>
        public delegate void RequestPacketReceived(RequestPacket packet);

        /// <summary>
        /// Delegate used for when a new packet comes, called after <see cref="PacketReceived"/>
        /// </summary>
        /// <param name="packet">The DataPacket received</param>
        public delegate void ResponsePacketReceived(ResponsePacket packet);

        /// <summary>
        /// Delegate used for when a new packet <typeparamref name="T"/> comes, called after <see cref="DataPacketReceived"/>
        /// </summary>
        /// <param name="packet">The DataPacket <typeparamref name="T"/> received</param>
        public delegate void GenericPacketReceived<T>(T packet) where T : IPacket;

        /// <summary>
        /// Delegated used for handling Log-type messages
        /// </summary>
        /// <param name="message"></param>
        public delegate void LogMessage(string message);

        /// <summary>
        /// Delegate used for handling DiscoveryResponse
        /// </summary>
        /// <param name="msg">The request message</param>
        /// <param name="handler">The <see cref="NetPeer"/> that is handling the request</param>
        /// <param name="response">The response to send back</param>
        /// <returns><see cref="bool"/> True if a discovery response should be sent, False if not</returns>
        public delegate bool DiscoveryResponse(NetIncomingMessage msg, NetPeer handler, out NetOutgoingMessage response);

        /// <summary>
        /// Delegate used for client authentication
        /// </summary>
        /// <param name="packet">The <see cref="ConnectPacket"/> send by the client</param>
        /// <returns><see cref="bool"/> that indicated if the client is authenticated</returns>
        public delegate bool ClientAuthenticaton(ConnectPacket packet);

        /// <summary>
        /// Delegate used for <see cref="IPacket"/> deserialization
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public delegate IPacket DeserializePacket(NetIncomingMessage message);

        /// <summary>
        /// Delegate used for handling DiscoveryRequest
        /// </summary>
        /// <param name="handler">The <see cref="NetPeer"/> that is handling the request</param>
        /// <param name="response">The request message to send</param>
        public delegate void DiscoveryRequest(NetPeer handler, out NetOutgoingMessage response);

        /// <summary>
        /// Delegate used for when client is authenticated
        /// </summary>
        /// <param name="client">The client's connection to server</param>
        public delegate void ClientAuthenticated(NetConnection client);

        /// <summary>
        /// Delegate used for when client's or server's status has changed
        /// </summary>
        /// <param name="connection">The <see cref="NetConnection"/> that status' has changed</param>
        /// <param name="status">The new status of the <see cref="NetConnection"/>'s <see cref="NetConnectionStatus"/> <paramref name="status"/></param>
        public delegate void StatusChanged(NetConnection connection, NetConnectionStatus status);

        /// <summary>
        /// Delegate used for when you are authenticated by server
        /// </summary>
        /// <param name="server">The server that authenticated</param>
        public delegate void Authenticated(NetConnection server);

        /// <summary>
        /// Delegate used for when a <see cref="RequestHandle"/> receives a <see cref="ResponsePacket"/> with matching Identifier
        /// </summary>
        /// <param name="response">The received <see cref="ResponsePacket"/></param>
        public delegate void ReceivedResponse(ResponsePacket response);

        /// <summary>
        /// Delegate used for when a <see cref=""/>
        /// </summary>
        /// <param name="handler">The <see cref="RequestHandle"/> that timed out</param>
        public delegate void RequestTimedOut(RequestHandle handler); 
    }
}
