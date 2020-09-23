using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToastedNetwork.PacketModel;
using ToastedNetwork.Packets;
using ToastedNetwork.Registry;
using static ToastedNetwork.Delegates;

namespace ToastedNetwork.NetworkUsers
{
    public class NetworkClient : INetworkUser
    {
        /// <summary>
        /// Returns the <see cref="Client"/>'s identifier
        /// </summary>
        public long Identifier => Client.UniqueIdentifier;

        /// <summary>
        /// Returns true if either the server is <see cref="NetPeerStatus.Running"/> or <see cref="NetPeerStatus.Starting"/>
        /// </summary>
        public bool Running => (Client.Status == NetPeerStatus.Running || Client.Status == NetPeerStatus.Starting);

        /// <summary>
        /// Returns true if we are connected to a remote host (<see cref="NetConnectionStatus.Connected"/>
        /// </summary>
        public bool Connected => (Client.ConnectionStatus == NetConnectionStatus.Connected);

        private PacketCallbackRegistry m_callbackRegistry { get; set; }
        private RequestHandleRegistry m_requestRegistry { get; set; }

        #region Events

        /// <summary>
        /// Called when an internal error occurs
        /// </summary>
        public event LogMessage OnError;

        /// <summary>
        /// Called when an error message is received
        /// </summary>
        public event LogMessage OnErrorMessage;

        /// <summary>
        /// Called when an warning message is received
        /// </summary>
        public event LogMessage OnWarningMessage;

        /// <summary>
        /// Called when an debug message is received
        /// </summary>
        public event LogMessage OnDebugMessage;

        /// <summary>
        /// Called when the <see cref="ServerConnection"/> changes status
        /// </summary>
        public event StatusChanged OnServerStatusChanged;

        /// <summary>
        /// Called when the <see cref="Client"/> connects to the <see cref="ServerConnection"/> (<see cref="NetConnectionStatus.Connected"/>
        /// </summary>
        public event ConnectedToServer OnConnected;

        /// <summary>
        /// Called when the <see cref="Client"/> is authenticated by <see cref="NetworkServer.ClientAuthenticaton"/>
        /// </summary>
        public event Authenticated OnAuthenticated;

        /// <summary>
        /// Called when a <see cref="IPacket"/> is received by the <see cref="Client"/>
        /// </summary>
        public event PacketReceived OnPacketReceived;

        /// <summary>
        /// Called when a <see cref="DataPacket"/> is received by the <see cref="Client"/>, called after <see cref="OnPacketReceived"/>
        /// </summary>
        public event DataPacketReceived OnDataPacketReceived;

        /// <summary>
        /// Called when a <see cref="PacketModel.RequestPacket"/> is received by the <see cref="Client"/>, called after <see cref="OnPacketReceived"/>
        /// </summary>
        public event RequestPacketReceived OnRequestPacketReceived;

        /// <summary>
        /// Called when a <see cref="ResponsePacket"/> is received by the <see cref="Client"/>, called after <see cref="OnPacketReceived"/>
        /// </summary>
        public event ResponsePacketReceived OnResponsePacketReceived;

        #endregion

        #region Callbacks

        /// <summary>
        /// Provides a callback used for deserializing raw <see cref="IPacket"/>'s binary data into a <see cref="IPacket"/>
        /// </summary>
        public DeserializePacket DeserializePacket { get; set; }

        #endregion

        /// <summary>
        /// The actual wrapped <see cref="NetClient"/> used for handling low level networking 
        /// (again, i was to lazy to write it myself. also this is ctrl+v from <see cref="NetworkServer"/>)
        /// </summary>
        public NetClient Client { get; private set; }

        /// <summary>
        /// The connection to the remote <see cref="NetServer"/>, <see langword="null"/> if we aren't connected
        /// </summary>
        public NetConnection ServerConnection { get => Client.ServerConnection; }

        /// <summary>
        /// Constructs an instance of <see cref="NetworkServer"/> with an Application Identifier <paramref name="appIdentifier"/>
        /// </summary>
        /// <param name="appIdentifier">Application Identifier used by <see cref="Server"/> for determining valid connection requests</param>
        public NetworkClient(string appIdentifier)
        {
            var config = new NetPeerConfiguration(appIdentifier)
            {
                EnableUPnP = true
            };

            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.Data);

            Client = new NetClient(config);
            DeserializePacket = v_DeserializePacket;
            m_callbackRegistry = new PacketCallbackRegistry();
            m_requestRegistry = new RequestHandleRegistry();

            Client.RegisterReceivedCallback(new SendOrPostCallback(v_ReceivedMessage), SynchronizationContext.Current);
        }

        /// <summary>
        /// Constructs an instance of <see cref="NetworkServer"/> with a default Application Identifier (<see cref="Network.AppIdentifier"/>)
        /// </summary>
        public NetworkClient() : this(Network.AppIdentifier)
        {
        }

        /// <summary>
        /// Start the <see cref="Client"/>
        /// </summary>
        /// <returns><see langword="True"/> if <see cref="Client"/> wasn't already running, <see langword="False"/> if the server is running (<see cref="Running"/>)</returns>
        public bool Start()
        {
            if (Running)
                return false;

            Client.Start();

            return true;
        }

        /// <summary>
        /// Sends a remote host with the <paramref name="endPoint"/> with a password <paramref name="password"/>
        /// </summary>
        /// <param name="endPoint">The <see cref="IPEndPoint"/> of the host</param>
        /// <param name="password">The string used for authentication by <see cref="NetworkServer.ClientAuthenticaton"/></param>
        public void Connect(IPEndPoint endPoint, string password)
        {
            Client.Disconnect("Disconnected");
            Client.Connect(endPoint, new ConnectPacket(password).CreateMessage(Client));
        }

        /// <summary>
        /// Shuts down the <see cref="Server"/> and send a string message containg a message
        /// </summary>
        /// <param name="closeMessage">The reason / message why you stopped the <see cref="Server"/></param>
        /// <returns><see langword="True"/> if the server was running (<see cref="Running"/>), <see langword="False"/> if the server was already shutdown</returns>
        public bool Stop(string closeMessage)
        {
            if (!Running)
                return false;

            Client.Shutdown(closeMessage);

            return true;
        }

        /// <summary>
        /// Sends an <see cref="IPacket"/> to the server using the specified <see cref="IPacket.DeliveryMethod"/>
        /// </summary>
        /// <param name="packet">The <see cref="IPacket"/> to send</param>
        public void SendPacket(IPacket packet)
        {
            ServerConnection.SendMessage(packet.CreateMessage(Client), Network.DeliveryMethod, Network.DefaultChannel);
        }

        /// <summary>
        /// Registers a <see cref="DataPacketReceived{T}"/> callback for when we receive a <see cref="DataPacket"/> of type <typeparamref name="TPacket"/>
        /// </summary>
        /// <typeparam name="TPacket">The packet we should callback on</typeparam>
        /// <param name="callback">The actual delegate used for the callback</param>
        public void RegisterPacketCallback<TPacket>(GenericPacketReceived<TPacket> callback) where TPacket : IPacket
        {
            m_callbackRegistry.RegisterPacketCallback(callback);
        }

        /// <summary>
        /// Unregisters a <see cref="DataPacketReceived{T}"/> callback matching <paramref name="callback"/>
        /// </summary>
        /// <typeparam name="TPacket">The packet that should be on the callback</typeparam>
        /// <param name="callback">The actual delegate to unregister</param>
        public void UnregisterPacketCallback<TPacket>(GenericPacketReceived<TPacket> callback) where TPacket : IPacket
        {
            m_callbackRegistry.UnregisterPacketCallback(callback);
        }

        /// <summary>
        /// Send a <see cref="PacketModel.RequestPacket"/> <paramref name="req"/> of type <typeparamref name="TReq"/> to the server 
        /// and awaits a <see cref="ResponsePacket"/> of type <typeparamref name="TResp"/>
        /// </summary>
        /// <typeparam name="TReq">The type of the packet to send</typeparam>
        /// <typeparam name="TResp">The type of the response to await</typeparam>
        /// <param name="req">The request packet</param>
        /// <returns></returns>
        public Task<TResp> RequestPacket<TReq, TResp>(TReq req) where TReq : RequestPacket where TResp : ResponsePacket
        {
            if (!req.ResponseType.IsType<TResp>())
                throw new Exception("Tried to request a packet with invalid response");

            bool timedOut = false;
            TResp resp = null;

            var handle = m_requestRegistry.RegisterResponse((packet) => resp = (TResp)packet, (handle) => timedOut = true, req);

            SendPacket(req); ;

            return Task.Factory.StartNew(() =>
            {
                while (resp == null)
                {
                    if (timedOut)
                        return null;

                    Thread.Sleep(50);
                }

                return resp;
            });
        }

        void INetworkUser.SendPacket(IPacket packet, IPEndPoint endPoint)
        {
            Client.SendUnconnectedMessage(packet.CreateMessage(Client), endPoint);
        }

        #region Private Functions

        private void v_ReceivedMessage(object peer)
        {
            NetIncomingMessage msg = Client.ReadMessage();

            switch (msg.MessageType)
            {
                case NetIncomingMessageType.Error:
                    OnError?.Invoke(msg.ReadString());
                    break;

                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
                    string why = msg.ReadString();

                    OnServerStatusChanged?.Invoke(msg.SenderConnection, status);

                    if ((NetConnectionStatus.Connected | NetConnectionStatus.RespondedConnect).HasFlag(status))
                        OnAuthenticated?.Invoke(ServerConnection);

                    break;

                case NetIncomingMessageType.Data:
                    v_HandlePacket(msg);
                    break;

                case NetIncomingMessageType.VerboseDebugMessage:
                case NetIncomingMessageType.DebugMessage:
                    OnDebugMessage?.Invoke(msg.ReadString());
                    break;

                case NetIncomingMessageType.WarningMessage:
                    OnWarningMessage?.Invoke(msg.ReadString());
                    break;

                case NetIncomingMessageType.ErrorMessage:
                    OnErrorMessage?.Invoke(msg.ReadString());
                    break;

                default:
                    break;
            }

            Client.Recycle(msg);
        }

        private IPacket v_DeserializePacket(NetIncomingMessage msg)
        {
            try
            {
                return Utils.DeserializePacket(msg);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.ToString());
                return null;
            }
        }

        private void v_HandlePacket(NetIncomingMessage msg)
        {
            try
            {
                IPacket packet = DeserializePacket(msg);

                if (packet is null)
                    throw new Exception("Tried to parse a IPacket, but failed");

                OnPacketReceived?.Invoke(packet);
                m_callbackRegistry.HandlePacket(packet);

                if (packet is DataPacket dataPacket)
                {
                    OnDataPacketReceived?.Invoke(dataPacket);
                }
                else if (packet is RequestPacket requestPacket)
                {
                    OnRequestPacketReceived?.Invoke(requestPacket);
                }
                else if (packet is ResponsePacket responsePacket)
                {
                    OnResponsePacketReceived?.Invoke(responsePacket);
                    m_requestRegistry.HandlePacket(responsePacket);
                }
            }
            catch (Exception e)
            {
                msg.SenderConnection.Deny(string.Format("Internal Error: {0}", e.ToString()));
            }
        }

        #endregion
    }
}
