using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using ToastedNetwork.PacketModel;
using ToastedNetwork.Packets;
using static ToastedNetwork.Delegates;

namespace ToastedNetwork.NetworkUsers
{
    /// <summary>
    /// A Server class wrapping around the <see cref="NetServer"/>
    /// </summary>
    public class NetworkServer : INetworkUser
    {
        /// <summary>
        /// Returns the <see cref="Server"/>'s identifier
        /// </summary>
        public long Identifier => Server.UniqueIdentifier;

        /// <summary>
        /// Returns true if either the server is <see cref="NetPeerStatus.Running"/> or <see cref="NetPeerStatus.Starting"/>
        /// </summary>
        public bool Running => (Server.Status == NetPeerStatus.Running || Server.Status == NetPeerStatus.Starting);

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
        /// Called when a client's <see cref="NetConnection"/> changes status
        /// </summary>
        public event StatusChanged OnClientStatusChanged;

        /// <summary>
        /// Called when a client's <see cref="NetConnectionStatus"/> is equal to <see cref="NetConnectionStatus.Connected"/>
        /// </summary>
        public event ClientConnected OnClientConnected;

        /// <summary>
        /// Called after a client is authenticated by <see cref="ClientAuthenticaton"/>
        /// </summary>
        public event ClientAuthenticated OnClientAuthenticated;

        /// <summary>
        /// Called when a <see cref="IPacket"/> is received by the <see cref="Server"/>
        /// </summary>
        public event PacketReceived OnPacketReceived;

        /// <summary>
        /// Called when a <see cref="DataPacket"/> is received by the <see cref="Server"/>, called after <see cref="OnPacketReceived"/>
        /// </summary>
        public event DataPacketReceived OnDataPacketReceived;

        /// <summary>
        /// Called when a <see cref="PacketModel.RequestPacket"/> is received by the <see cref="Server"/>, called after <see cref="OnPacketReceived"/>
        /// </summary>
        public event RequestPacketReceived OnRequestPacketReceived;

        /// <summary>
        /// Called when a <see cref="ResponsePacket"/> is received by the <see cref="Server"/>, called after <see cref="OnPacketReceived"/>
        /// </summary>
        public event ResponsePacketReceived OnResponsePacketReceived;

        #endregion

        #region Callbacks

        /// <summary>
        /// Provides a callback used for client authentication
        /// </summary>
        public ClientAuthenticaton ClientAuthenticaton { get; set; }

        /// <summary>
        /// Provides a callback used for determining if we should send a discovery response message
        /// </summary>
        public DiscoveryResponse DiscoveryResponse { get; set; }

        /// <summary>
        /// Provides a callback used for deserializing raw <see cref="IPacket"/>'s binary data into a <see cref="IPacket"/>
        /// </summary>
        public DeserializePacket DeserializePacket { get; set; }

        #endregion

        /// <summary>
        /// The actual wrapped <see cref="NetServer"/> used for handling low level networking (i was to lazy to write it myself)
        /// </summary>
        public NetServer Server { get; private set; }

        /// <summary>
        /// Constructs an instance of <see cref="NetworkServer"/> with a Port <paramref name="port"/> and an Application Identifier <paramref name="appIdentifier"/>
        /// </summary>
        /// <param name="appIdentifier">Application Identifier used by <see cref="Server"/> for determining valid connection requests</param>
        /// <param name="port">A port on which <see cref="Server"/> listens for messages, please don't use these ports unless you need to <see cref="DefaultPorts"/></param>
        public NetworkServer(string appIdentifier, int port)
        {
            var config = new NetPeerConfiguration(appIdentifier)
            {
                Port = port,
                EnableUPnP = true
            };

            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            config.EnableMessageType(NetIncomingMessageType.Data);

            Server = new NetServer(config);
            m_callbackRegistry = new PacketCallbackRegistry();
            m_requestRegistry = new RequestHandleRegistry();

            ClientAuthenticaton = v_DefaultAuth;
            DiscoveryResponse = v_DiscoveryResponse;
            DeserializePacket = v_DeserializePacket;

            //Server.UPnP = new NetUPnP.ForwardPort(port, appIdentifier + " server on port " + port);

            SynchronizationContext context = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context);

            Server.RegisterReceivedCallback(new SendOrPostCallback(v_ReceivedMessage), SynchronizationContext.Current);
        }

        /// <summary>
        /// Constructs an instance of <see cref="NetworkServer"/> with a Port <paramref name="port"/> and a default Application Identifier (<see cref="Network.AppIdentifier"/>)
        /// </summary>
        /// <param name="port">A port on which <see cref="Server"/> listens for messages, please don't use these ports unless you need to <see cref="DefaultPorts"/></param>
        public NetworkServer(int port) : this(Network.AppIdentifier, port)
        {
        }

        /// <summary>
        /// Start the <see cref="Server"/>
        /// </summary>
        /// <returns><see langword="True"/> if <see cref="Server"/> wasn't already running, <see langword="False"/> if the server is running (<see cref="Running"/>)</returns>
        public bool Start()
        {
            if (Running)
                return false;

            Server.Start();

            return true;
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

            Server.Shutdown(closeMessage);

            return true;
        }

        /// <summary>
        /// Sends an <see cref="IPacket"/> to a client using the specified <see cref="IPacket.DeliveryMethod"/>
        /// </summary>
        /// <param name="packet">The <see cref="IPacket"/> to send</param>
        /// <param name="connection">The client's <see cref="NetConnection"/></param>
        public void SendPacket(IPacket packet, NetConnection connection)
        {
            connection.SendMessage(packet.CreateMessage(Server), Network.DeliveryMethod, Network.DefaultChannel);
        }

        void INetworkUser.SendPacket(IPacket packet, IPEndPoint endPoint)
        {
            Server.SendUnconnectedMessage(packet.CreateMessage(Server), endPoint);
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
        /// Send a <see cref="PacketModel.RequestPacket"/> <paramref name="req"/> of type <typeparamref name="TReq"/> to a <paramref name="client"/> 
        /// and awaits a <see cref="ResponsePacket"/> of type <typeparamref name="TResp"/>
        /// </summary>
        /// <typeparam name="TReq">The type of the packet to send</typeparam>
        /// <typeparam name="TResp">The type of the response to await</typeparam>
        /// <param name="req">The request packet</param>
        /// <param name="client">The client to send the request to</param>
        /// <returns></returns>
        public Task<TResp> RequestPacket<TReq, TResp>(TReq req, NetConnection client) where TReq : RequestPacket where TResp : ResponsePacket
        {
            if (!req.ResponseType.IsType<TResp>())
                throw new Exception("Tried to request a packet with invalid response");

            bool timedOut = false;
            TResp resp = null;

            var handle = m_requestRegistry.RegisterResponse((packet) => resp = (TResp)packet, (handle) => timedOut = true, req);

            SendPacket(req, client); ;

            return Task.Factory.StartNew(() =>
            {
                while (!timedOut)
                {
                    if (resp == null)
                        return null;

                    Thread.Sleep(50);
                }

                return resp;
            });
        }

        #region Private Functions

        private void v_ReceivedMessage(object peer)
        {
            NetIncomingMessage msg = Server.ReadMessage();

            switch (msg.MessageType)
            {
                case NetIncomingMessageType.Error:
                    OnError?.Invoke(msg.ReadString());

                    break;



                case NetIncomingMessageType.StatusChanged:
                    OnClientStatusChanged?.Invoke(msg.SenderConnection, msg.SenderConnection.Status);

                    if (msg.SenderConnection.Status == NetConnectionStatus.Connected)
                        OnClientConnected?.Invoke(msg.SenderConnection);

                    break;


                case NetIncomingMessageType.ConnectionApproval:
                    v_HandleConnectionAuth(msg);

                    break;


                case NetIncomingMessageType.Data:
                    v_HandlePacket(msg);

                    break;


                case NetIncomingMessageType.DiscoveryRequest:
                    if (!DiscoveryResponse(msg, Server, out NetOutgoingMessage resp))
                        break;

                    Server.SendDiscoveryResponse(resp, msg.SenderEndPoint);

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
            }
            Server.Recycle(msg);
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

        private void v_HandleConnectionAuth(NetIncomingMessage msg)
        {
            try
            {
                IPacket packet = Utils.DeserializePacket(msg);

                if (!(packet is ConnectPacket connect))
                    throw new Exception("Received incorrect message packet");

                if (ClientAuthenticaton(connect))
                {
                    msg.SenderConnection.Approve();
                    OnClientAuthenticated?.Invoke(msg.SenderConnection);
                }
                else
                {
                    msg.SenderConnection.Deny("Failed to authenticate connection");
                }
            }
            catch (Exception e)
            {
                msg.SenderConnection.Deny(string.Format("Internal Error: {0}", e.ToString()));
            }
        }

        private bool v_DiscoveryResponse(NetIncomingMessage msg, NetPeer handler, out NetOutgoingMessage response)
        {
            response = handler.CreateMessage();
            msg.Write(Server.ConnectionsCount);

            return true;
        }

        private bool v_DefaultAuth(ConnectPacket packet)
        {
            return Network.ShouldAuthWhenDefault;
        }

        #endregion
    }
}
