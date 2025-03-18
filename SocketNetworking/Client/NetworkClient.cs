using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.Misc;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Authentication;
using SocketNetworking.Shared.Events;
using SocketNetworking.Shared.Messages;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.Streams;
using SocketNetworking.Shared.Transports;

namespace SocketNetworking.Client
{
    public class NetworkClient
    {
        static NetworkClient instance;

        /// <summary>
        /// Gets the local singleton for the <see cref="NetworkClient"/>. Can only be called on the client, multiple <see cref="NetworkClient"/>s on the local context are not recommended. Technically, you can have as many as you'd like. But you would be restricted only having <see cref="NetworkInvoke(string, object[], bool)"/>, as <see cref="INetworkObject"/>s would collide.
        /// </summary>
        public static NetworkClient LocalClient
        {
            get
            {
                if (NetworkManager.WhereAmI == ClientLocation.Remote)
                {
                    throw new InvalidOperationException("Cannot get local client from the server.");
                }
                return instance;
            }
        }

        public NetworkClient()
        {
            Log = new Log()
            {
                Prefix = $"[Client (No ID)]"
            };
            ClientCreated?.Invoke(this);
            _networkEncryptionManager = new NetworkEncryption(this);
            _streams = new NetworkStreams(this);
            PrivateInit();
            Init();
        }

        /// <summary>
        /// Forcefully remove client on destruction
        /// </summary>
        ~NetworkClient()
        {
            ClientDestroyed?.Invoke(ClientID);
            if (Clients.Contains(this))
            {
                Clients.Remove(this);
            }
            instance = null;
        }

        #region Per Client (Non-Static) Events

        public event Action<INetworkAvatar> AvatarChanged;

        public event Action<long> LatencyChanged;

        protected void InvokeLatencyChanged(long value)
        {
            LatencyChanged?.Invoke(value);
        }

        /// <summary>
        /// Called when <see cref="Authenticated"/> changes states.
        /// </summary>
        public event Action AuthenticationStateChanged;

        /// <summary>
        /// Called on both Remote and Local clients when the connection has succeeded and the Socket is ready to use.
        /// </summary>
        public event Action ClientConnected;

        /// <summary>
        /// Called on the both Remote and Local clients when the connection stops. Note that Remote Clients will be destroyed.
        /// </summary>
        public event Action ClientDisconnected;

        /// <summary>
        /// Called when the <see cref="StopClient"/> finishes executing.
        /// </summary>
        public event Action ClientStopped;

        /// <summary>
        /// Called on both Remote and Local Clients when the connection state changes.
        /// </summary>
        public event Action<ConnectionState> ConnectionStateUpdated;

        /// <summary>
        /// Called on server and clients when an error is raised.
        /// </summary>
        public event Action<NetworkErrorData> ConnectionError;

        protected void InvokeConnectionError(NetworkErrorData data)
        {
            ConnectionError?.Invoke(data);
        }


        /// <summary>
        /// Called when the state of the <see cref="NetworkClient.Ready"/> variable changes. First variable is the old state, and the second is the new state. This event is fired on both Local and Remote clients.
        /// </summary>
        public event Action<bool, bool> ReadyStateChanged;

        /// <summary>
        /// Called on Local and Remote clients when a full packet is read.
        /// </summary>
        public event Action<PacketHeader, byte[]> PacketRead;

        protected void InvokePacketRead(PacketHeader header, byte[] data)
        {
            PacketRead?.Invoke(header, data);
        }

        /// <summary>
        /// Called when a packet is ready to handle, this event is never called if <see cref="NetworkClient.ManualPacketHandle"/> is set to false
        /// </summary>
        public event Action<PacketHeader, byte[]> PacketReadyToHandle;

        protected void InvokePacketReadyToHandle(PacketHeader header, byte[] data)
        {
            PacketReadyToHandle?.Invoke(header, data);
        }

        /// <summary>
        /// Called when a packet is ready to send, this event is never called if <see cref="NetworkClient.ManualPacketSend"/> is set to false.
        /// </summary>
        public event Action<Packet> PacketReadyToSend;

        /// <summary>
        /// Called when the Client ID is changed on the server or client, note that it is only invoked on the server when the <see cref="ServerDataPacket"/> has already been sent, meaning the client should be aware of its own <see cref="ClientID"/>, assuming packets arrive in order.
        /// </summary>
        public event Action ClientIdUpdated;

        /// <summary>
        /// Called when the <see cref="EncryptionState"/> has been set to <see cref="EncryptionState.SymmetricalReady"/> or higher.
        /// </summary>
        public event Action EncryptionComplete;

        /// <summary>
        /// Called when a <see cref="Packet"/> is about to be sent. if <see cref="ChoiceEvent.Accepted"/> is false, this is cancelled. See <see cref="ChoiceEvent.Accept"/> and <see cref="ChoiceEvent.Reject"/>
        /// </summary>
        public event EventHandler<PacketSendRequest> PacketSendRequest;

        protected bool InvokePacketSendRequest(Packet packet)
        {
            PacketSendRequest req = new PacketSendRequest(packet);
            PacketSendRequest?.Invoke(this, req);
            return req.Accepted;
        }

        /// <summary>
        /// Called when a <see cref="Packet"/> has been sent.
        /// </summary>
        public event Action<Packet> PacketSent;

        protected void InvokePacketSent(Packet packet)
        {
            PacketSent?.Invoke(packet);
        }

        #endregion

        #region Static Events

        /// <summary>
        /// Called when any clients <see cref="Ready"/> state changes
        /// </summary>
        public static event Action<NetworkClient> ClientReadyStateChanged;

        /// <summary>
        /// Called when any clients <see cref="CurrentConnectionState"/> is changed
        /// </summary>
        public static event Action<NetworkClient> ClientConnectionStateChanged;

        /// <summary>
        /// Called when a network client is destroyed, gives the clients ID.
        /// </summary>
        public static event Action<int> ClientDestroyed;

        /// <summary>
        /// Called when <see cref="StopClient"/> finishes.
        /// </summary>
        public static event Action<NetworkClient> ClientStoppedStatic;

        /// <summary>
        /// Called when a client is created, gives the <see cref="NetworkClient"/> that was created.
        /// </summary>
        public static event Action<NetworkClient> ClientCreated;

        #endregion

        #region Properties

        /// <summary>
        /// Maximum amount of milliseconds before a <see cref="KeepAlivePacket"/> is sent. By default, a <see cref="KeepAlivePacket"/> is sent every 1000ms, this is the standard for ping measurements.
        /// </summary>
        public double MaxMSBeforeKeepAlive { get; set; } = 1000;

        /// <summary>
        /// The calculated latency. Use <see cref="CheckLatency"/> to forcibly update this. You should see <see cref="MaxMSBeforeKeepAlive"/>.
        /// </summary>
        public long Latency => _latency;

        /// <summary>
        /// Does this client support SSL? By default, only <see cref="TcpNetworkClient"/>s and <see cref="MixedNetworkClient"/>s support SSL.
        /// </summary>
        public virtual bool SupportsSSL { get; } = false;

        /// <summary>
        /// Provides the <see cref="SocketNetworking.Log"/> instance for this client. The <see cref="Log.Prefix"/> is set to contain the client ID for logging purposes.
        /// </summary>
        public Log Log { get; }

        INetworkAvatar _avatar;

        /// <summary>
        /// The Avatar of the <see cref="NetworkClient"/>. This can be specified in 
        /// </summary>
        public INetworkAvatar Avatar
        {
            get => _avatar;
            private set
            {
                _avatar = value;
                AvatarChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// Only has instances on the local client. Use <see cref="NetworkServer.ConnectedClients"/> for server side clients.
        /// </summary>
        public readonly static HashSet<NetworkClient> Clients = new HashSet<NetworkClient>();


        private int _clientId = 0;

        /// <summary>
        /// The clients Network Synced ID
        /// </summary>
        public int ClientID
        {
            get
            {
                return _clientId;
            }
        }

        private NetworkTransport _transport;

        /// <summary>
        /// The <see cref="NetworkTransport"/> that is being used primarily.
        /// </summary>
        public virtual NetworkTransport Transport
        {
            get
            {
                return _transport;
            }
            set
            {
                _transport = value;
            }
        }

        protected NetworkEncryption _networkEncryptionManager;

        /// <summary>
        /// The <see cref="NetworkEncryption"/> class handles public and private keygen, as well as decrypting and encrypting packets.
        /// </summary>
        public NetworkEncryption EncryptionManager
        {
            get
            {
                return _networkEncryptionManager;
            }
        }

        protected NetworkStreams _streams;

        /// <summary>
        /// The <see cref="NetworkStreams"/> class handles <see cref="NetworkSyncedStream"/>s and their subclasses.
        /// </summary>
        public NetworkStreams Streams
        {
            get
            {
                return _streams;
            }
        }

        protected AuthenticationProvider _provider;

        /// <summary>
        /// The <see cref="SocketNetworking.Shared.Authentication.AuthenticationProvider"/> Handles authentication between the server and client. It is useful for passwords, characters, tokens, etc. See <see cref="Authenticated"/>.
        /// </summary>
        public AuthenticationProvider AuthenticationProvider
        {
            get
            {
                return _provider;
            }
            set
            {
                _provider = value;
            }
        }

        /// <summary>
        /// Returns the Address to which the socket is connected too, In the format IP:Port
        /// </summary>
        public string ConnectedIPAndPort
        {
            get
            {
                IPEndPoint remoteIpEndPoint = Transport.Peer;
                return $"{remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}";
            }
        }

        /// <summary>
        /// Returns the connection IP
        /// </summary>
        public string ConnectedHostname
        {
            get
            {
                IPEndPoint remoteIpEndPoint = Transport.Peer;
                return $"{remoteIpEndPoint.Address}";
            }
        }

        /// <summary>
        /// Returns the connection port
        /// </summary>
        public int ConnectedPort
        {
            get
            {
                IPEndPoint remoteIpEndPoint = Transport.Peer;
                return remoteIpEndPoint.Port;
            }
        }

        private bool _authenticated = false;

        /// <summary>
        /// Determines if the <see cref="NetworkClient"/> has proved itself based on the <see cref="AuthenticationProvider"/>. This property can be set to any value at any time, and is synced accordingly. (Server -> Client)
        /// </summary>
        public bool Authenticated
        {
            get
            {
                return _authenticated;
            }
            set
            {
                if (CurrentClientLocation == ClientLocation.Local)
                {
                    throw new InvalidOperationException("Can't change authentication state on the client!");
                }
                NetworkInvoke(nameof(ClientGetAuthenticatedState), new object[] { value });
                _authenticated = value;
                AuthenticationStateChanged?.Invoke();
            }
        }

        [NetworkInvokable(NetworkDirection.Server)]
        private void ClientGetAuthenticatedState(NetworkHandle handle, bool state)
        {
            Log.Info("Authentication has been successful.");
            _authenticated = state;
            AuthenticationStateChanged?.Invoke();
        }


        private bool _ready = false;

        /// <summary>
        /// Determines the ready state of the <see cref="NetworkClient"/>, this has no effect on library logic but can be useful for applications using the library. Wrong. it has an effect. <see cref="NetworkServerConfig.AutoSync"/> is checked when the <see cref="Ready"/> state is updated to true. This value can be updated without extra networking logic as it is updated for you.
        /// </summary>
        public bool Ready
        {
            get
            {
                if (!IsConnected)
                {
                    return false;
                }
                return _ready;
            }
            set
            {
                if (!IsTransportConnected || CurrentConnectionState != ConnectionState.Connected)
                {
                    Log.Warning("Can't change ready state because the socket is not connected or the handshake isn't done.");
                    return;
                }
                ReadyStateUpdatePacket readyStateUpdatePacket = new ReadyStateUpdatePacket
                {
                    Ready = value
                };
                Send(readyStateUpdatePacket);
                if (CurrentClientLocation == ClientLocation.Remote)
                {
                    _ready = value;
                    ReadyStateChanged?.Invoke(!_ready, _ready);
                    ClientReadyStateChanged?.Invoke(this);
                    NetworkManager.SendReadyPulse(this, Ready);
                }
                if (value)
                {
                    Log.Success("Client Ready.");
                }
            }
        }


        private bool _manualPacketHandle = false;

        private bool _manualPacketSend = false;

        /// <summary>
        /// If true, the library will not handle packets automatically instead queueing them, you must call <see cref="NetworkClient.HandleNextPacket"/> to handle the next packet.
        /// </summary>
        public bool ManualPacketHandle
        {
            get => _manualPacketHandle;
            set
            {
                if (CurrentConnectionState == ConnectionState.Handshake)
                {
                    Log.Warning("Changing Packet read mode while the handshake has not yet finished, this may cause issues!");
                }
                _manualPacketHandle = value;
            }
        }

        /// <summary>
        /// Prevents the library from automatically sending packets, instead waiting for a call to <see cref="NetworkClient.SendNextPacket()"/>
        /// </summary>
        public bool ManualPacketSend
        {
            get => _manualPacketSend;
            set
            {
                if (CurrentConnectionState == ConnectionState.Handshake)
                {
                    Log.Warning("Changing Packet write mode while in handshake, things may break!");
                }
                _manualPacketSend = value;
            }
        }

        /// <summary>
        /// <see cref="bool"/> which determines if the client has connected to a server
        /// </summary>
        public bool IsTransportConnected
        {
            get
            {
                if (Transport == null)
                {
                    return false;
                }
                if (CurrentClientLocation == ClientLocation.Remote && Transport.IsConnected)
                {
                    return true;
                }
                return Transport != null && Transport.IsConnected;
            }
        }

        /// <summary>
        /// Determines if the client is connected to anything.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return (CurrentConnectionState == ConnectionState.Connected || CurrentConnectionState == ConnectionState.Handshake) && Transport.IsConnected;
            }
            set
            {
                if (!value)
                {
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// <see cref="bool"/> which represents if the Client has been started
        /// </summary>
        private bool ClientStarted => CurrentClientLocation == ClientLocation.Remote || _clientActive;

        private ConnectionState _connectionState = ConnectionState.Disconnected;

        /// <summary>
        /// The <see cref="ConnectionState"/> of the current client. Can only be set by clients which have the <see cref="ClientLocation.Remote"/> <see cref="CurrentClientLocation"/>
        /// </summary>
        public ConnectionState CurrentConnectionState
        {
            get => _connectionState;
            set
            {
                if (CurrentClientLocation != ClientLocation.Remote)
                {
                    Log.Error("Local client tried changing state of connection, only servers can do so.");
                    return;
                }
                ConnectionUpdatePacket updatePacket = new ConnectionUpdatePacket
                {
                    State = value,
                    Reason = "Setter in remote."
                };
                SendImmediate(updatePacket);
                _connectionState = value;
                ConnectionStateUpdated?.Invoke(value);
                ClientConnectionStateChanged?.Invoke(this);
                NetworkManager.SendConnectedPulse(this);
                if (value == ConnectionState.Connected)
                {
                    Log.Success("Handshake Successful.");
                }
            }
        }

        private EncryptionState _encryptionState = EncryptionState.Disabled;

        /// <summary>
        /// Determines the <see cref="Shared.EncryptionState"/> of the current client.
        /// </summary>
        public EncryptionState EncryptionState
        {
            get
            {
                return _encryptionState;
            }
            protected set
            {
                _encryptionState = value;
                //Log.Debug($"Encryption State Updated: {_encryptionState}, As Number: {(int)_encryptionState}");
                if (value >= EncryptionState.SymmetricalReady)
                {
                    EncryptionComplete?.Invoke();
                }
            }
        }

        private bool _clientActive = false;

        private Thread _packetSenderThread;

        /// <summary>
        /// The Clients <see cref="Thread"/> which handles sending all packets.
        /// </summary>
        public Thread PacketSenderThread
        {
            get
            {
                return _packetSenderThread;
            }
        }

        private Thread _packetReaderThread;

        /// <summary>
        /// The Clients <see cref="Thread"/> which handles reading all packets
        /// </summary>
        public Thread PacketReaderThread
        {
            get
            {
                return _packetReaderThread;
            }
        }

        private ClientLocation _clientLocation = ClientLocation.Local;

        /// <summary>
        /// The current location of the client. Remote = On the server, Local = On the local client
        /// </summary>
        public ClientLocation CurrentClientLocation
        {
            get
            {
                return _clientLocation;
            }
        }


        private ProtocolConfiguration _clientConfiguration = new ProtocolConfiguration();

        /// <summary>
        /// Represents expected protocol and version from server. Note that the set statement will fail is the client is connected to a server.
        /// </summary>
        public ProtocolConfiguration ClientConfiguration
        {
            get
            {
                return _clientConfiguration;
            }
            set
            {
                if (IsTransportConnected)
                {
                    Log.Error("Can't update NetworkConfiguration while client is connected.");
                    return;
                }
                _clientConfiguration = value;
            }
        }


        protected bool _shuttingDown = false;

        /// <summary>
        /// This boolean is set to true when the <see cref="StopClient"/> function is called. It is intended to be a kill switch for the client.
        /// </summary>
        public bool ShuttingDown => _shuttingDown;

        /// <summary>
        /// Returns the amount of <see cref="Packet"/>s left to read, this is always zero if <see cref="NetworkClient.ManualPacketHandle"/> is false
        /// </summary>
        public int PacketsLeftToRead
        {
            get
            {
                return _toReadPackets.Count;
            }
        }

        /// <summary>
        /// Returns the amount of Packets left to send.
        /// </summary>
        public int PacketsLeftToSend
        {
            get
            {
                return _toSendPackets.Count;
            }
        }

        private bool _noPacketHandling = false;

        /// <summary>
        /// When true, no <see cref="Packet"/>s can be sent or Received by the <see cref="NetworkClient"/>. This is usually used with the <see cref="SupportsSSL"/> property to ensure proper SSL communication.
        /// </summary>
        public bool NoPacketHandling
        {
            get
            {
                return _noPacketHandling;
            }
            set
            {
                _noPacketHandling = value;
            }
        }

        private bool _noPacketSending = false;

        /// <summary>
        /// Prevents the current <see cref="NetworkClient"/> from sending any <see cref="Packet"/>s, Except with <see cref="SendImmediate(Packet)"/>.
        /// </summary>
        public bool NoPacketSending
        {
            get
            {
                return _noPacketSending;
            }
            set
            {
                _noPacketSending = value;
            }
        }

        #endregion

        #region Encryption

        /// <summary>
        /// Requests encryption from the remote server.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> if the remote server has its encryption state as <see cref="ServerEncryptionMode.Disabled"/>, true otherwise.
        /// </returns>
        public bool ClientRequestEncryption()
        {
            return NetworkInvokeBlocking<bool>(nameof(ServerGetEncryptionRequest), new object[] { });
        }

        [NetworkInvokable(NetworkDirection.Client)]
        private bool ServerGetEncryptionRequest()
        {
            if (NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Disabled)
            {
                return false;
            }
            if (NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Required)
            {
                return true;
            }
            ServerBeginEncryption();
            return true;
        }

        /// <summary>
        /// Forces the server to begin encryption with the local client. If the client fails to handshake in time, they will be disconnected. Calling this method on the local client or with encryption active does nothing.
        /// </summary>
        public void ServerBeginEncryption()
        {
            if (CurrentClientLocation != ClientLocation.Remote || EncryptionState > EncryptionState.Disabled)
            {
                return;
            }
            EncryptionPacket packet = new EncryptionPacket
            {
                EncryptionFunction = EncryptionFunction.AsymmetricalKeySend,
                PublicKey = EncryptionManager.MyPublicKey
            };
            _encryptionState = EncryptionState.Handshake;
            Send(packet);
            CallbackTimer<NetworkClient> timer = new CallbackTimer<NetworkClient>((x) =>
            {
                int encryptionState = (int)x.EncryptionState;
                if (encryptionState < 2)
                {
                    x.Disconnect("Failed Encryption Handshake.");
                }
            }, this, 10f);
            timer.Start();
        }

        protected virtual void ConfirmSSL()
        {
            throw new NotImplementedException();
        }

        protected virtual bool ClientTrySSLUpgrade()
        {
            throw new NotImplementedException();
        }

        protected virtual bool ServerTrySSLUpgrade()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Init

        /// <summary>
        /// Called on the server and client when a client is created.
        /// </summary>
        public virtual void Init()
        {

        }


        private void PrivateInit()
        {

        }

        /// <summary>
        /// Used when initializing a <see cref="NetworkClient"/> object on the server. Do not call this on the local client.
        /// </summary>
        /// <param name="clientId">
        /// Given ClientID
        /// </param>
        /// <param name="socket">
        /// The <see cref="NetworkTransport"/> object which handles data transport.
        /// </param>
        public virtual void InitRemoteClient(int clientId, NetworkTransport socket)
        {
            _clientId = clientId;
            Log.Prefix = $"[Client {clientId}]";
            Transport = socket;
            _clientLocation = ClientLocation.Remote;
            if (NetworkServer.Authenticator != null)
            {
                AuthenticationProvider = (AuthenticationProvider)Activator.CreateInstance(NetworkServer.Authenticator);
            }
            ClientConnected += OnRemoteClientConnected;
            ClientConnected?.Invoke();
            ReadyStateChanged += OnReadyStateChanged;
            //_packetReaderThread = new Thread(PacketReaderThreadMethod);
            //_packetReaderThread.Start();
            //_packetSenderThread = new Thread(PacketSenderThreadMethod);
            //_packetSenderThread.Start();
        }

        /// <summary>
        /// Should be called locally to initialize the client, switching it from just being created to being ready to be used.
        /// </summary>
        public virtual void InitLocalClient()
        {
            if (instance != null && instance != this)
            {
                throw new InvalidOperationException("Having several active clients is not allowed.");
            }
            instance = this;
            _clientLocation = ClientLocation.Local;
            ClientConnected += OnLocalClientConnected;
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Attempts to connect an IP and port. Note that if this operation fails, nothing is started, meaning you can call this method again without extra cleanup. Calling this method however, instantly drops the current socket. Do NOT call this if your client is already connected. Can only be run on the Local client.
        /// </summary>
        /// <param name="hostname">
        /// A <see cref="string"/> of the hostname.
        /// </param>
        /// <param name="port">
        /// An <see cref="ushort"/> representation of the port
        /// </param>
        /// <returns>
        /// A <see cref="bool"/> indicating connection success. Note this only returns the status of the socket connection, not of the full connection action. E.g. you can still fail to connect if the server refuses to accept the client.
        /// </returns>
        public bool Connect(string hostname, ushort port)
        {
            if (CurrentClientLocation == ClientLocation.Remote)
            {
                Log.Error("Cannot connect to other servers from remote.");
                return false;
            }
            if (IsTransportConnected)
            {
                Log.Error("Can't connect: Already connected to a server.");
                return false;
            }
            string finalHostname;
            if (!IPAddress.TryParse(hostname, out IPAddress ip))
            {
                try
                {
                    IPHostEntry entry = Dns.GetHostEntry(hostname);
                    if (entry.AddressList.Count() == 0)
                    {
                        Log.Error($"Can't find host {hostname}");
                        return false;
                    }
                    else
                    {
                        finalHostname = entry.AddressList[0].ToString();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("DNS Resolution failed. " + ex.ToString());
                    return false;
                }
            }
            else
            {
                finalHostname = hostname;
            }
            Log.Info($"Connecting to {finalHostname}:{port}...");
            try
            {
                Exception ex = Transport.Connect(finalHostname, port);
                if (ex != null)
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                NetworkErrorData networkErrorData = new NetworkErrorData("Connection Failed: " + ex.ToString(), false);
                ConnectionError?.Invoke(networkErrorData);
                Log.Error($"Failed to connect: \n {ex}");
                return false;
            }
            StartClient();
            return true;
        }

        /// <summary>
        /// Send a disconnect message to the other party and kill local client
        /// </summary>
        /// <param name="message">
        /// A <see cref="string"/> which to send as a message to the other party.
        /// </param>
        public virtual void Disconnect(string message)
        {
            ConnectionUpdatePacket connectionUpdatePacket = new ConnectionUpdatePacket
            {
                State = ConnectionState.Disconnected,
                Reason = message
            };
            SendImmediate(connectionUpdatePacket);
            _connectionState = ConnectionState.Disconnected;
            NetworkErrorData errorData = new NetworkErrorData("Disconnected. Reason: " + connectionUpdatePacket.Reason, false);
            ConnectionError?.Invoke(errorData);
            ClientDisconnected?.Invoke();
            if (CurrentClientLocation == ClientLocation.Remote)
            {
                Log.Info($"Disconnecting Client {ClientID} for " + message);
            }
            if (CurrentClientLocation == ClientLocation.Local)
            {
                Log.Info("Disconnecting from server. Reason: " + message);
            }
            StopClient();
        }

        /// <summary>
        /// Disconnects the connection with the reason "Disconnected"
        /// </summary>
        public void Disconnect()
        {
            Disconnect("Disconnected");
        }

        /// <summary>
        /// Stops the client, removing the Thread and closing the socket
        /// </summary>
        public virtual void StopClient()
        {
            if (_shuttingDown)
            {
                return;
            }
            Log.Info("Closing Client!");
            _shuttingDown = true;
            NoPacketHandling = true;
            _connectionState = ConnectionState.Disconnected;
            Transport?.Close();
            if (CurrentClientLocation == ClientLocation.Remote)
            {
                OnRemoteStopClient();
            }
            else
            {
                OnLocalStopClient();
            }
            _toReadPackets = null;
            _toSendPackets = null;
            if (Clients.Contains(this))
            {
                Clients.Remove(this);
            }
            ClientStopped?.Invoke();
            ClientStoppedStatic?.Invoke(this);
            GC.Collect();
        }

        protected virtual void OnLocalStopClient()
        {
            NetworkManager.SendDisconnectedPulse(this);
            _packetReaderThread?.Abort();
            _packetSenderThread?.Abort();
            _packetReaderThread = null;
            _packetSenderThread = null;
        }

        protected virtual void OnRemoteStopClient()
        {
            NetworkServer.RemoveClient(this);
        }

        void StartClient()
        {
            if (CurrentClientLocation == ClientLocation.Remote)
            {
                Log.Error("Can't start client on remote, started by constructor.");
                return;
            }
            if (ClientStarted)
            {
                Log.Error("Can't start client, already started.");
                return;
            }
            Log.Info("Starting client!");
            _packetReaderThread?.Abort();
            _packetReaderThread = new Thread(PacketReaderThreadMethod);
            _packetSenderThread?.Abort();
            _packetSenderThread = new Thread(PacketSenderThreadMethod);
            _clientActive = true;
            _shuttingDown = false;
            _packetReaderThread.Start();
            _packetSenderThread.Start();
            _toReadPackets = new ConcurrentQueue<ReadPacketInfo>();
            _toSendPackets = new ConcurrentQueue<Packet>();
            ClientConnected?.Invoke();
            Clients.Add(this);
        }

        #endregion

        #region Packet Sending

        /// <summary>
        /// Sends the next <see cref="Packet"/> from the send queue. If <see cref="ManualPacketSend"/> is false, this method does nothing.
        /// </summary>
        public void SendNextPacket()
        {
            if (!ManualPacketSend)
            {
                return;
            }
            SendNextPacketInternal();
        }

        /// <summary>
        /// Forces the library to send the provided packet immediately on the calling thread. This is not a good idea, and should not be used.
        /// </summary>
        /// <param name="packet"></param>
        public virtual void SendImmediate(Packet packet)
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (!Transport.IsConnected)
            {
                return;
            }
            PreparePacket(ref packet);
            byte[] fullBytes = SerializePacket(packet);
            try
            {
                Exception ex = Transport.Send(fullBytes, packet.Destination);
                if (ex != null)
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to send packet! Error:\n" + ex.ToString());
                NetworkErrorData networkErrorData = new NetworkErrorData("Failed to send packet: " + ex.ToString(), true);
                ConnectionError?.Invoke(networkErrorData);
            }
        }

        /// <summary>
        /// Queues a <see cref="Packet"/> to be sent through the <see cref="Transport"/>.
        /// If <see cref="IsTransportConnected"/> is false, this method will return early, not sending anything.
        /// </summary>
        /// <param name="packet"></param>
        public void Send(Packet packet)
        {
            if (!IsTransportConnected)
            {
                Log.Warning("Can't Send packet, not connected!");
                ConnectionError?.Invoke(new NetworkErrorData("Tried to send packets while not connected.", IsTransportConnected));
                return;
            }
            else
            {
                _toSendPackets.Enqueue(packet);
                if (ManualPacketSend)
                {
                    PacketReadyToSend?.Invoke(packet);
                }
            }
        }


        /// <summary>
        /// Sends a <see cref="Packet"/> as if directed from/to a <see cref="INetworkObject"/>. Internally calls <see cref="Send(Packet)"/>.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="sender"></param>
        public void Send(TargetedPacket packet, INetworkObject sender)
        {
            packet.NetworkIDTarget = sender.NetworkID;
            Send(packet);
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> with the <see cref="PacketFlags.Priority"/> flag set to the <paramref name="priority"/> value. Internally calls <see cref="Send(Packet)"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="priority"></param>
        public void Send(Packet packet, bool priority)
        {
            packet.Flags = packet.Flags.SetFlag(PacketFlags.Priority, priority);
            Send(packet);
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> with the <see cref="PacketFlags.Priority"/> flag set to the <paramref name="priority"/> value, and the <see cref="Packet.NetowrkIDTarget"/> is set to the <see cref="INetworkObject.NetworkID"/> of the <paramref name="sender"/>. Internally calls <see cref="Send(Packet)"/>.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="sender"></param>
        /// <param name="priority"></param>
        public void Send(TargetedPacket packet, INetworkObject sender, bool priority)
        {
            packet.NetworkIDTarget = sender.NetworkID;
            packet.Flags = packet.Flags.SetFlag(PacketFlags.Priority, priority);
            Send(packet);
        }

        #endregion

        #region Network Invoke

        /// <summary>
        /// Preforms a blocking Network Invocation (Like an RPC) and attempts to return you a value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="maxTimeMs"></param>
        /// <returns></returns>
        public T NetworkInvoke<T>(object target, string methodName, object[] args, float maxTimeMs = 5000, bool priority = false)
        {
            return NetworkManager.NetworkInvokeBlocking<T>(target, this, methodName, args, maxTimeMs, priority);
        }

        /// <summary>
        /// Preforms a non-blocking Network Invocation (Like an RPC)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public void NetworkInvoke(object target, string methodName, object[] args, bool priority = false)
        {
            NetworkManager.NetworkInvoke(target, this, methodName, args, priority);
        }

        /// <summary>
        /// Preforms a non-blocking Network Invocation (Like an RPC). This will try to find the method on the current <see cref="NetworkClient"/>
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public void NetworkInvoke(string methodName, object[] args, bool priority = false)
        {
            NetworkManager.NetworkInvoke(this, this, methodName, args, priority);
        }

        /// <summary>
        /// Preforms a blocking Network Invocation (Like an RPC) and attempts to return you a value. This will try to find the method on the current <see cref="NetworkClient"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="maxTimeMs"></param>
        /// <returns></returns>
        public T NetworkInvokeBlocking<T>(string methodName, object[] args, float maxTimeMs = 5000, bool priority = false)
        {
            return NetworkManager.NetworkInvokeBlocking<T>(this, this, methodName, args, maxTimeMs, priority);
        }

        /// <summary>
        /// Preforms a non-blocking Network Invocation and returns a <see cref="NetworkInvocationCallback{T}"/> which contains a <see cref="NetworkInvocationCallback{T}.Callback"/> which you can listen to get the return value (if any). 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public NetworkInvocationCallback<T> NetworkInvoke<T>(string methodName, object[] args, bool priority = false)
        {
            return NetworkManager.NetworkInvoke<T>(this, this, methodName, args, priority);
        }

        #endregion

        #region Internal Events

        void OnLocalClientConnected()
        {
            if (CurrentClientLocation != ClientLocation.Local)
            {
                return;
            }
            ClientDataPacket dataPacket = new ClientDataPacket
            {
                Configuration = ClientConfiguration
            };
            Send(dataPacket);
        }

        void OnRemoteClientConnected()
        {
            if (CurrentClientLocation != ClientLocation.Remote)
            {
                return;
            }
            CurrentConnectionState = ConnectionState.Handshake;
        }

        #endregion

        #region Sending/receiving

        #region Sending

        protected object streamLock = new object();

        protected ConcurrentQueue<Packet> _toSendPackets = new ConcurrentQueue<Packet>();

        protected virtual void SendNextPacketInternal()
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (NoPacketSending)
            {
                return;
            }
            if (_toSendPackets.IsEmpty)
            {
                return;
            }
            lock (streamLock)
            {
                _toSendPackets.TryDequeue(out Packet packet);
                PreparePacket(ref packet);
                if (!InvokePacketSendRequest(packet))
                {
                    return;
                }
                byte[] fullBytes = SerializePacket(packet);
                try
                {
                    //Log.Debug($"Sending packet: {packet.ToString()}");
                    Exception ex = Transport.Send(fullBytes, packet.Destination);
                    if (ex != null)
                    {
                        throw ex;
                    }
                    //Log.Debug("Packet sent!");
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to send packet! Error:\n" + ex.ToString());
                    NetworkErrorData networkErrorData = new NetworkErrorData("Failed to send packet: " + ex.ToString(), true);
                    ConnectionError?.Invoke(networkErrorData);
                }
                InvokePacketSent(packet);
            }
        }

        /// <summary>
        /// Called before serialization, if the base is not called, the <see cref="Packet.Source"/> will be null.
        /// </summary>
        /// <param name="packet"></param>
        protected virtual void PreparePacket(ref Packet packet)
        {
            packet.Source = Transport.LocalEndPoint;
            packet.SendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            //Port 0 is not a valid port.
            if (packet.Destination.Port == 0)
            {
                packet.Destination = Transport.Peer;
            }
            bool validationSuccess = packet.ValidatePacket();
            if (!validationSuccess)
            {
                Log.Error($"Invalid packet: {packet}");
            }
        }

        /// <summary>
        /// Serializes the packet. This method also applies Encryption as per <see cref="EncryptionState"/> and flags according to <see cref="Packet.Flags"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        protected virtual byte[] SerializePacket(Packet packet)
        {
            int currentEncryptionState = (int)EncryptionState;
            if (currentEncryptionState >= (int)EncryptionState.SymmetricalReady)
            {
                //Log.Debug("Encrypting using SYMMETRICAL");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.SymetricalEncrypted, true);
            }
            else if (currentEncryptionState >= (int)EncryptionState.AsymmetricalReady)
            {
                //Log.Debug("Encrypting using ASYMMETRICAL");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.AsymetricalEncrypted, true);
            }
            else
            {
                //Ensure the packet isn't encrypted if we don't support it.
                //Log.Debug("Encryption is not supported at this moment, ensuring it isn't flagged as being enabled on this packet.");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.AsymetricalEncrypted, false);
                packet.Flags = packet.Flags.SetFlag(PacketFlags.SymetricalEncrypted, false);
            }
            if (!packet.ValidateFlags())
            {
                Log.Error($"Packet send failed! {packet}");
                return null;
            }
            //Log.Debug("Active Flags: " + string.Join(", ", packet.Flags.GetActiveFlags()));
            byte[] packetBytes = packet.Serialize().Data;
            byte[] packetHeaderBytes = packetBytes.Take(PacketHeader.HeaderLength - 4).ToArray();
            byte[] packetDataBytes = packetBytes.Skip(PacketHeader.HeaderLength - 4).ToArray();
            //StringBuilder hex = new StringBuilder(packetBytes.Length * 2);
            //Log.Debug("Raw Serialized Packet: \n" + hex.ToString());
            if (packet.Flags.HasFlag(PacketFlags.Compressed))
            {
                //Log.Debug("Compressing the packet.");
                packetDataBytes = packetDataBytes.Compress();
            }
            if (packet.Flags.HasFlag(PacketFlags.AsymetricalEncrypted))
            {
                if (currentEncryptionState < (int)EncryptionState.AsymmetricalReady)
                {
                    Log.Error("Encryption cannot be done at this point: Not ready.");
                    return null;
                }
                if (packetDataBytes.Length > NetworkEncryption.MaxBytesForAsym)
                {
                    Log.Warning($"Packet is too large for RSA! Packet Size: {packetDataBytes.Length}, Max Packet Size: {NetworkEncryption.MaxBytesForAsym}");
                }
                else
                {
                    //Log.Debug("Encrypting Packet: Asymmetrical");
                    packetDataBytes = EncryptionManager.Encrypt(packetDataBytes, false);
                }
            }
            if (packet.Flags.HasFlag(PacketFlags.SymetricalEncrypted))
            {
                if (currentEncryptionState < (int)EncryptionState.SymmetricalReady)
                {
                    Log.Error("Encryption cannot be done at this point: Not ready.");
                    return null;
                }
                //Log.Debug("Encrypting Packet: Symmetrical");
                packetDataBytes = EncryptionManager.Encrypt(packetDataBytes);
                if (packetDataBytes.Length == 0)
                {
                    Log.Error("Encryption resulted in a null!");
                    return null;
                }
            }
            ByteWriter writer = new ByteWriter();
            byte[] packetFull = packetHeaderBytes.Concat(packetDataBytes).ToArray();
            //Log.Debug($"Packet Size: Full (Raw): {packetBytes.Length}, Full (Processed): {packetFull.Length}. With Header Size: {packetFull.Length + 4}");
            writer.WriteInt(packetFull.Length);
            writer.Write(packetFull);
            int written = writer.Length;
            if (written != (packetFull.Length + 4))
            {
                Log.Error($"Trying to send corrupted size! {packet}");
                return null;
            }
            byte[] fullBytes = writer.Data;
            if (fullBytes.Length > Packet.MaxPacketSize)
            {
                Log.Error("Packet too large!");
                return null;
            }
            return fullBytes;
        }

        void PacketSenderThreadMethod()
        {
            while (!_shuttingDown)
            {
                RawWriter();
            }
        }

        protected void RawWriter()
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (_manualPacketSend)
            {
                return;
            }
            SendNextPacketInternal();
        }

        #endregion

        #region Receiving

        protected ConcurrentQueue<ReadPacketInfo> _toReadPackets = new ConcurrentQueue<ReadPacketInfo>();

        /// <summary>
        /// Method handling all <see cref="NetworkTransport"/> reading IO.
        /// </summary>
        protected virtual void PacketReaderThreadMethod()
        {
            while (!_shuttingDown)
            {
                RawReader();
            }
        }

        /// <summary>
        /// Method which reads actual data and processes it from the Network I/O, this is a blocking, single read method, it will not attempt to keep reading if there is not data on the <see cref="Transport"/>.
        /// </summary>
        protected virtual void RawReader()
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (!IsTransportConnected)
            {
                StopClient();
                return;
            }
            DoLatencyCheck();
            if (!Transport.DataAvailable)
            {
                return;
            }
            (byte[], Exception, IPEndPoint) packet = Transport.Receive();
            if (packet.Item1 == null)
            {
                Log.Warning("Transport received a null byte array.");
                return;
            }
            try
            {
                Deserialize(packet.Item1, packet.Item3);
            }
            catch (Exception ex)
            {
                Log.Warning($"Malformed Packet. Size: {packet.Item1.Length}, Error: {ex}");
            }
        }

        /// <summary>
        /// Called when deserializing the packet, preforms all functions needed to get the proper packet object and queue it for processing.
        /// </summary>
        /// <param name="fullPacket"></param>
        /// <param name="endpoint"></param>
        protected virtual void Deserialize(byte[] fullPacket, IPEndPoint endpoint)
        {
            //StringBuilder hex = new StringBuilder(fullPacket.Length * 2);
            //Log.Debug(hex.ToString());
            PacketHeader header = Packet.ReadPacketHeader(fullPacket);
            //Log.Debug("Active Flags: " + string.Join(", ", header.Flags.GetActiveFlags()));
            //Log.Debug($"Inbound Packet Info, Size Of Full Packet: {header.Size}, Type: {header.Type}, Target: {header.NetworkIDTarget}, CustomPacketID: {header.CustomPacketID}");
            byte[] rawPacket = fullPacket;
            byte[] headerBytes = fullPacket.Take(PacketHeader.HeaderLength).ToArray();
            byte[] packetBytes = fullPacket.Skip(PacketHeader.HeaderLength).ToArray();
            int currentEncryptionState = (int)EncryptionState;
            if (header.Flags.HasFlag(PacketFlags.SymetricalEncrypted))
            {
                //Log.Debug("Trying to decrypt a packet using SYMMETRICAL encryption!");
                if (currentEncryptionState < (int)EncryptionState.SymmetricalReady)
                {
                    Log.Error("Encryption cannot be done at this point: Not ready.");
                    return;
                }
                packetBytes = EncryptionManager.Decrypt(packetBytes);
            }
            if (header.Flags.HasFlag(PacketFlags.AsymetricalEncrypted))
            {
                //Log.Debug("Trying to decrypt a packet using ASYMMETRICAL encryption!");
                if (currentEncryptionState < (int)EncryptionState.AsymmetricalReady)
                {
                    Log.Error("Encryption cannot be done at this point: Not ready.");
                    return;
                }
                packetBytes = EncryptionManager.Decrypt(packetBytes, false);
            }
            if (header.Flags.HasFlag(PacketFlags.Compressed))
            {
                packetBytes = packetBytes.Decompress();
            }
            if (header.Size + 4 < fullPacket.Length)
            {
                Log.Warning($"Header provided size is less then the actual packet length! Header: {header.Size}, Actual Packet Size: {fullPacket.Length - 4}");
            }
            fullPacket = headerBytes.Concat(packetBytes).ToArray();
            //StringBuilder hex1 = new StringBuilder(fullPacket.Length * 2);
            //Log.Debug("Raw Deserialized Packet: \n" + hex1.ToString());
            PacketRead?.Invoke(header, fullPacket);
            if (ManualPacketHandle)
            {
                ReadPacketInfo packetInfo = new ReadPacketInfo()
                {
                    Header = header,
                    Data = fullPacket
                };
                _toReadPackets.Enqueue(packetInfo);
                PacketReadyToHandle?.Invoke(packetInfo.Header, packetInfo.Data);
            }
            else
            {
                HandlePacket(header, fullPacket);
            }
        }

        #region Handle Packets

        /// <summary>
        /// Only called on the server.
        /// </summary>
        protected virtual void HandleRemoteClient(PacketHeader header, byte[] data)
        {
            switch (header.Type)
            {
                case PacketType.Authentication:
                    AuthenticationPacket authenticationPacket = new AuthenticationPacket();
                    authenticationPacket.Deserialize(data);
                    _ = Task.Run(() => 
                    {
                        if (AuthenticationProvider == null)
                        {
                            Log.Warning("Got an authentication request but not AuthenticationProvider is specified.");
                            return;
                        }
                        NetworkHandle handle = new NetworkHandle(this);
                        if (authenticationPacket.IsResult)
                        {
                            AuthenticationProvider.HandleAuthenticationResult(handle, authenticationPacket);
                            Authenticated = authenticationPacket.Result.Approved;
                        }
                        else
                        {
                            (AuthenticationResult, byte[]) result = AuthenticationProvider.Authenticate(handle, authenticationPacket);
                            AuthenticationPacket newPacket = new AuthenticationPacket
                            {
                                IsResult = true,
                                Result = result.Item1,
                                ExtraAuthenticationData = result.Item2
                            };
                            Send(newPacket);
                            Authenticated = newPacket.Result.Approved;
                        }
                    });
                    break;
                case PacketType.ClientToClient:
                    ClientToClientPacket clientToClientPacket = new ClientToClientPacket();
                    clientToClientPacket.Deserialize(data);
                    INetworkObject obj = NetworkManager.GetNetworkObjectByID(clientToClientPacket.NetworkIDTarget).Item1;
                    if (obj == null)
                    {
                        return;
                    }
                    NetworkClient owner = obj.GetOwner();
                    if (owner == null)
                    {
                        return;
                    }
                    owner.Send(clientToClientPacket);
                    break;
                case PacketType.KeepAlive:
                    KeepAlivePacket keepAlivePacket = new KeepAlivePacket();
                    keepAlivePacket.Deserialize(data);
                    DoLatency(keepAlivePacket);
                    break;
                case PacketType.Stream:
                    StreamPacket streamPacket = new StreamPacket();
                    streamPacket.Deserialize(data);
                    Streams.HandlePacket(streamPacket);
                    break;
                case PacketType.Custom:
                    NetworkManager.TriggerPacketListeners(header, data, this);
                    break;
                case PacketType.ObjectManage:
                    ObjectManagePacket objectManagePacket = new ObjectManagePacket();
                    objectManagePacket.Deserialize(data);
                    NetworkHandle networkHandle = new NetworkHandle(this, objectManagePacket);
                    try
                    {
                        NetworkManager.ModifyNetworkObjectLocal(objectManagePacket, networkHandle);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        SendLog(ex.Message, LogSeverity.Error);
                    }
                    break;
                case PacketType.SyncVarUpdate:
                    SyncVarUpdatePacket syncVarUpdate = new SyncVarUpdatePacket();
                    syncVarUpdate.Deserialize(data);
                    try
                    {
                        NetworkManager.UpdateSyncVarsInternal(syncVarUpdate, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        SendLog(ex.Message, LogSeverity.Error);
                    }
                    break;
                case PacketType.SSLUpgrade:
                    SSLUpgradePacket sslUpgradePacket = new SSLUpgradePacket();
                    sslUpgradePacket.Deserialize(data);
                    if (sslUpgradePacket.Result)
                    {
                        SSLUpgradePacket sslUpgradeResponse = new SSLUpgradePacket()
                        {
                            Continue = true,
                        };
                        SendImmediate(sslUpgradeResponse);
                        ConfirmSSL();
                        NoPacketSending = false;
                        if (NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Required)
                        {
                            ServerBeginEncryption();
                        }
                        else if (NetworkServer.Config.DefaultReady && NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Disabled)
                        {
                            Ready = true;
                        }
                    }
                    else
                    {
                        Disconnect("SSL Handshake failure");
                    }
                    break;
                case PacketType.ConnectionStateUpdate:
                    ConnectionUpdatePacket connectionUpdatePacket = new ConnectionUpdatePacket();
                    connectionUpdatePacket.Deserialize(data);
                    if (connectionUpdatePacket.State == ConnectionState.Disconnected)
                    {
                        //ruh roh
                        NetworkErrorData errorData = new NetworkErrorData("Disconnected by local client. Reason: " + connectionUpdatePacket.Reason, false);
                        ConnectionError?.Invoke(errorData);
                        Log.Error($"Disconnecting {ClientID} for " + connectionUpdatePacket.Reason);
                        StopClient();
                    }
                    if (connectionUpdatePacket.State == ConnectionState.Handshake)
                    {
                        _connectionState = ConnectionState.Handshake;
                    }
                    if (connectionUpdatePacket.State == ConnectionState.Connected)
                    {
                        _connectionState = ConnectionState.Connected;
                        NetworkManager.SendConnectedPulse(this);
                    }
                    ClientConnectionStateChanged?.Invoke(this);
                    ConnectionStateUpdated?.Invoke(_connectionState);
                    break;
                case PacketType.ReadyStateUpdate:
                    ReadyStateUpdatePacket readyStateUpdatePacket = new ReadyStateUpdatePacket();
                    readyStateUpdatePacket.Deserialize(data);
                    if (NetworkServer.AllowClientSelfReady)
                    {
                        Ready = readyStateUpdatePacket.Ready;
                    }
                    break;
                case PacketType.ClientData:
                    ClientDataPacket clientDataPacket = new ClientDataPacket();
                    clientDataPacket.Deserialize(data);
                    if (clientDataPacket.Configuration.Protocol != NetworkServer.ServerConfiguration.Protocol)
                    {
                        Disconnect($"Server protocol mismatch. Expected: {NetworkServer.ServerConfiguration.Protocol} Got: {clientDataPacket.Configuration.Protocol}");
                        break;
                    }
                    if (clientDataPacket.Configuration.Version != NetworkServer.ServerConfiguration.Version)
                    {
                        Disconnect($"Server protocol mismatch. Expected: {NetworkServer.ServerConfiguration.Version} Got: {clientDataPacket.Configuration.Version}");
                        break;
                    }
                    ServerDataPacket serverDataPacket = new ServerDataPacket
                    {
                        YourClientID = _clientId,
                        Configuration = NetworkServer.ServerConfiguration,
                        CustomPacketAutoPairs = NetworkManager.PacketPairsSerialized,
                        UpgradeToSSL = NetworkServer.Config.Certificate != null && SupportsSSL,
                    };
                    SendImmediate(serverDataPacket);
                    if (serverDataPacket.UpgradeToSSL && SupportsSSL)
                    {
                        NoPacketSending = true;
                        SSLUpgradePacket upgradePacket = new SSLUpgradePacket();
                        SendImmediate(upgradePacket);
                        ServerTrySSLUpgrade();
                    }
                    else
                    {
                        if (NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Required)
                        {
                            ServerBeginEncryption();
                        }
                        else if (NetworkServer.Config.DefaultReady && NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Disabled)
                        {
                            Ready = true;
                        }
                    }
                    CurrentConnectionState = ConnectionState.Connected;
                    ClientIdUpdated?.Invoke();
                    break;
                case PacketType.NetworkInvocation:
                    NetworkInvokationPacket networkInvocationPacket = new NetworkInvokationPacket();
                    networkInvocationPacket.Deserialize(data);
                    //Log.Debug($"Network Invocation: ObjectID: {networkInvocationPacket.NetworkObjectTarget}, Method: {networkInvocationPacket.MethodName}, Arguments Count: {networkInvocationPacket.Arguments.Count}");
                    try
                    {
                        NetworkManager.NetworkInvoke(networkInvocationPacket, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Network Invocation Failed! Method {networkInvocationPacket.MethodName}, Error: {ex}");
                        NetworkInvokationResultPacket errorPacket = new NetworkInvokationResultPacket
                        {
                            Success = false,
                            ErrorMessage = $"Method: {networkInvocationPacket.MethodName} Message: " + ex.Message,
                            Result = SerializedData.NullData,
                            CallbackID = networkInvocationPacket.CallbackID,
                            IgnoreResult = false
                        };
                        Send(errorPacket);
                    }
                    break;
                case PacketType.NetworkInvocationResult:
                    NetworkInvokationResultPacket networkInvocationResultPacket = new NetworkInvokationResultPacket();
                    networkInvocationResultPacket.Deserialize(data);
                    //Log.Debug($"NetworkInvocationResult: CallbackID: {networkInvocationResultPacket.CallbackID}, Success?: {networkInvocationResultPacket.Success}, Error Message: {networkInvocationResultPacket.ErrorMessage}");
                    NetworkManager.NetworkInvoke(networkInvocationResultPacket, this);
                    break;
                case PacketType.Encryption:
                    EncryptionPacket encryptionPacket = new EncryptionPacket();
                    encryptionPacket.Deserialize(data);
                    //Log.Info($"Encryption request! Function {encryptionPacket.EncryptionFunction}");
                    switch (encryptionPacket.EncryptionFunction)
                    {
                        case EncryptionFunction.None:
                            break;
                        case EncryptionFunction.AsymmetricalKeySend:
                            EncryptionManager.OthersPublicKey = encryptionPacket.PublicKey;
                            EncryptionPacket gotYourPublicKey = new EncryptionPacket
                            {
                                EncryptionFunction = EncryptionFunction.AsymmetricalKeyReceive
                            };
                            Send(gotYourPublicKey);
                            EncryptionState = EncryptionState.AsymmetricalReady;
                            //Log.Info($"Got Asymmetrical Encryption Key, ID: {ClientID}");
                            EncryptionPacket sendSymKey = new EncryptionPacket
                            {
                                EncryptionFunction = EncryptionFunction.SymmetricalKeySend,
                                SymKey = EncryptionManager.SharedAesKey.Item1,
                                SymIV = EncryptionManager.SharedAesKey.Item2
                            };
                            Send(sendSymKey);
                            break;
                        case EncryptionFunction.SymmetricalKeySend:
                            Disconnect("Illegal encryption handshake: Cannot send own symmetry key, wait for server.");
                            break;
                        case EncryptionFunction.AsymmetricalKeyReceive:
                            //Log.Info($"Client Got Asymmetrical Encryption Key, ID: {ClientID}");
                            break;
                        case EncryptionFunction.SymetricalKeyReceive:
                            EncryptionState = EncryptionState.SymmetricalReady;
                            //Log.Info($"Client Got Symmetrical Encryption Key, ID: {ClientID}");
                            EncryptionPacket updateEncryptionStateFinal = new EncryptionPacket
                            {
                                EncryptionFunction = EncryptionFunction.UpdateEncryptionStatus,
                                State = EncryptionState.Encrypted
                            };
                            Send(updateEncryptionStateFinal);
                            if (AuthenticationProvider != null && !AuthenticationProvider.ClientInitiate)
                            {
                                AuthenticationPacket AuthenticationPacket = AuthenticationProvider.BeginAuthentication();
                                Send(AuthenticationPacket);
                            }
                            Log.Success("Encryption Successful.");
                            if (NetworkServer.Config.DefaultReady == true)
                            {
                                Ready = true;
                            }
                            break;
                        default:
                            Log.Error($"Invalid Encryption function: {encryptionPacket.EncryptionFunction}");
                            break;
                    }
                    break;
                default:
                    Log.Error($"Packet is not handled! Info: Type: {header.Type}");
                    Disconnect("Client Sent an Unknown packet with PacketID " + header.Type.ToString());
                    break;
            }
        }

        /// <summary>
        /// Only called on the client.
        /// </summary>
        protected virtual void HandleLocalClient(PacketHeader header, byte[] data)
        {
            switch (header.Type)
            {
                case PacketType.Authentication:
                    AuthenticationPacket authenticationPacket = new AuthenticationPacket();
                    authenticationPacket.Deserialize(data);
                    _ = Task.Run(() => 
                    {
                        if (AuthenticationProvider == null)
                        {
                            Log.Warning("Got an authentication request but not AuthenticationProvider is specified.");
                            return;
                        }
                        NetworkHandle handle = new NetworkHandle(this);
                        if (authenticationPacket.IsResult)
                        {
                            AuthenticationProvider.HandleAuthenticationResult(handle, authenticationPacket);
                        }
                        else
                        {
                            (AuthenticationResult, byte[]) result = AuthenticationProvider.Authenticate(handle, authenticationPacket);
                            AuthenticationPacket newPacket = new AuthenticationPacket
                            {
                                IsResult = true,
                                Result = result.Item1,
                                ExtraAuthenticationData = result.Item2
                            };
                            Send(newPacket);
                        }
                    });
                    break;
                case PacketType.ClientToClient:
                    ClientToClientPacket clientToClientPacket = new ClientToClientPacket();
                    clientToClientPacket.Deserialize(data);
                    if (Avatar == null)
                    {
                        return;
                    }
                    if (Avatar.NetworkID != clientToClientPacket.NetworkIDTarget)
                    {
                        return;
                    }
                    Avatar.ReceivePrivate(clientToClientPacket);
                    break;
                case PacketType.KeepAlive:
                    KeepAlivePacket keepAlivePacket = new KeepAlivePacket();
                    keepAlivePacket.Deserialize(data);
                    DoLatency(keepAlivePacket);
                    break;
                case PacketType.Stream:
                    StreamPacket streamPacket = new StreamPacket();
                    streamPacket.Deserialize(data);
                    Streams.HandlePacket(streamPacket);
                    break;
                case PacketType.Custom:
                    NetworkManager.TriggerPacketListeners(header, data, this);
                    break;
                case PacketType.ObjectManage:
                    ObjectManagePacket objectManagePacket = new ObjectManagePacket();
                    objectManagePacket.Deserialize(data);
                    NetworkHandle networkHandle = new NetworkHandle(this, objectManagePacket);
                    try
                    {
                        NetworkManager.ModifyNetworkObjectLocal(objectManagePacket, networkHandle);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        SendLog(ex.Message, LogSeverity.Error);
                    }
                    break;
                case PacketType.SyncVarUpdate:
                    SyncVarUpdatePacket syncVarUpdate = new SyncVarUpdatePacket();
                    syncVarUpdate.Deserialize(data);
                    try
                    {
                        NetworkManager.UpdateSyncVarsInternal(syncVarUpdate, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        SendLog(ex.Message, LogSeverity.Error);
                    }
                    break;
                case PacketType.ReadyStateUpdate:
                    ReadyStateUpdatePacket readyStateUpdatePacket = new ReadyStateUpdatePacket();
                    readyStateUpdatePacket.Deserialize(data);
                    _ready = readyStateUpdatePacket.Ready;
                    ReadyStateChanged?.Invoke(!_ready, _ready);
                    ClientReadyStateChanged?.Invoke(this);
                    NetworkManager.SendReadyPulse(this, Ready);
                    Log.Info("New Client Ready State: " + _ready.ToString());
                    break;
                case PacketType.ServerData:
                    ServerDataPacket serverDataPacket = new ServerDataPacket();
                    serverDataPacket.Deserialize(data);
                    _clientId = serverDataPacket.YourClientID;
                    Log.Prefix = $"[Client {_clientId}]";
                    Log.Info("New Client ID: " + _clientId.ToString());
                    Log.Info("Server Supports SSL? " + serverDataPacket.UpgradeToSSL);
                    if (serverDataPacket.Configuration.Protocol != ClientConfiguration.Protocol || serverDataPacket.Configuration.Version != ClientConfiguration.Version)
                    {
                        Disconnect($"Server protocol mismatch. Expected: {ClientConfiguration} Got: {serverDataPacket.Configuration}");
                        break;
                    }
                    ClientIdUpdated?.Invoke();
                    Dictionary<int, string> newPacketPairs = serverDataPacket.CustomPacketAutoPairs;
                    List<Type> homelessPackets = new List<Type>();
                    foreach (int i in newPacketPairs.Keys)
                    {
                        Type t = NetworkManager.AdditionalPacketTypes.Values.FirstOrDefault(x => x.FullName == newPacketPairs[i]);
                        if (t == null)
                        {
                            Log.Error($"Can't find packet with name {newPacketPairs[i]}, this will cause more errors later!");
                            continue;
                        }
                        if (NetworkManager.AdditionalPacketTypes.ContainsKey(i))
                        {
                            if (!NetworkManager.IsDynamicAllocatedPacket(t))
                            {
                                Log.Error("Tried to overwrite non-dynamic packet. Type: " + t.FullName);
                                continue;
                            }
                            if (homelessPackets.Contains(t))
                            {
                                homelessPackets.Remove(t);
                            }
                            else
                            {
                                homelessPackets.Add(NetworkManager.AdditionalPacketTypes[i]);
                            }
                            NetworkManager.AdditionalPacketTypes[i] = t;
                        }
                        else
                        {
                            NetworkManager.AdditionalPacketTypes.Add(i, t);
                        }
                    }
                    string built = "\n";
                    foreach (int i in NetworkManager.AdditionalPacketTypes.Keys)
                    {
                        built += $"ID: {i}, Full Name: {NetworkManager.AdditionalPacketTypes[i].FullName}\n";
                    }
                    Log.Info("Finished re-writing dynamic packets: " + built);
                    break;
                case PacketType.SSLUpgrade:
                    SSLUpgradePacket ssLUpgradePacket = new SSLUpgradePacket();
                    ssLUpgradePacket.Deserialize(data);
                    if (ssLUpgradePacket.Continue)
                    {
                        ConfirmSSL();
                        NoPacketSending = false;
                        break;
                    }
                    else
                    {
                        NoPacketSending = true;
                        bool attemptResult = ClientTrySSLUpgrade();
                        SSLUpgradePacket upgradePacketResult = new SSLUpgradePacket()
                        {
                            Result = attemptResult,
                        };
                        if (!attemptResult)
                        {
                            NoPacketSending = false;
                            Disconnect("SSL Handshake failure");
                        }
                        SendImmediate(upgradePacketResult);
                    }
                    break;
                case PacketType.ConnectionStateUpdate:
                    ConnectionUpdatePacket connectionUpdatePacket = new ConnectionUpdatePacket();
                    connectionUpdatePacket.Deserialize(data);
                    Log.Info("New connection state: " + connectionUpdatePacket.State.ToString());
                    if (connectionUpdatePacket.State == ConnectionState.Disconnected)
                    {
                        //ruh roh
                        NetworkErrorData errorData = new NetworkErrorData("Disconnected by remote client. Reason: " + connectionUpdatePacket.Reason, false);
                        ConnectionError?.Invoke(errorData);
                        Log.Error("Disconnected: " + connectionUpdatePacket.Reason);
                        StopClient();
                    }
                    if (connectionUpdatePacket.State == ConnectionState.Handshake)
                    {
                        _connectionState = ConnectionState.Handshake;
                    }
                    if (connectionUpdatePacket.State == ConnectionState.Connected)
                    {
                        _connectionState = ConnectionState.Connected;
                        NetworkManager.SendConnectedPulse(this);
                    }
                    ClientConnectionStateChanged?.Invoke(this);
                    ConnectionStateUpdated?.Invoke(_connectionState);
                    break;
                case PacketType.NetworkInvocation:
                    NetworkInvokationPacket networkInvocationPacket = new NetworkInvokationPacket();
                    networkInvocationPacket.Deserialize(data);
                    //Log.Debug($"Network Invocation: ObjectID: {networkInvocationPacket.NetworkObjectTarget}, Method: {networkInvocationPacket.MethodName}, Arguments Count: {networkInvocationPacket.Arguments.Count}");
                    try
                    {
                        NetworkManager.NetworkInvoke(networkInvocationPacket, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Network Invocation Failed! Method {networkInvocationPacket.MethodName}, Error: {ex}");
                        NetworkInvokationResultPacket errorPacket = new NetworkInvokationResultPacket
                        {
                            Success = false,
                            ErrorMessage = $"Method: {networkInvocationPacket.MethodName} Message: " + ex.Message,
                            Result = SerializedData.NullData,
                            CallbackID = networkInvocationPacket.CallbackID,
                            IgnoreResult = false
                        };
                        Send(errorPacket);
                    }
                    break;
                case PacketType.NetworkInvocationResult:
                    NetworkInvokationResultPacket networkInvocationResultPacket = new NetworkInvokationResultPacket();
                    networkInvocationResultPacket.Deserialize(data);
                    //Log.Debug($"NetworkInvocationResult: CallbackID: {networkInvocationResultPacket.CallbackID}, Success?: {networkInvocationResultPacket.Success}, Error Message: {networkInvocationResultPacket.ErrorMessage}");
                    NetworkManager.NetworkInvoke(networkInvocationResultPacket, this);
                    break;
                case PacketType.Encryption:
                    EncryptionPacket encryptionPacket = new EncryptionPacket();
                    encryptionPacket.Deserialize(data);
                    EncryptionPacket encryptionReceive = new EncryptionPacket();
                    //Log.Info($"Encryption request! Function {encryptionPacket.EncryptionFunction}");
                    switch (encryptionPacket.EncryptionFunction)
                    {
                        case EncryptionFunction.None:
                            break;
                        case EncryptionFunction.AsymmetricalKeySend:
                            EncryptionManager.OthersPublicKey = encryptionPacket.PublicKey;
                            EncryptionPacket gotYourPublicKey = new EncryptionPacket
                            {
                                EncryptionFunction = EncryptionFunction.AsymmetricalKeyReceive
                            };
                            //Log.Info("Got Servers Public key, Sending mine.");
                            Send(gotYourPublicKey);
                            EncryptionState = EncryptionState.AsymmetricalReady;
                            encryptionReceive.PublicKey = EncryptionManager.MyPublicKey;
                            encryptionReceive.EncryptionFunction = EncryptionFunction.AsymmetricalKeySend;
                            Send(encryptionReceive);
                            break;
                        case EncryptionFunction.SymmetricalKeySend:
                            //Log.Info($"Got servers symmetrical key.");
                            EncryptionManager.SharedAesKey = new Tuple<byte[], byte[]>(encryptionPacket.SymKey, encryptionPacket.SymIV);
                            EncryptionPacket gotYourSymmetricalKey = new EncryptionPacket
                            {
                                EncryptionFunction = EncryptionFunction.SymetricalKeyReceive
                            };
                            Send(gotYourSymmetricalKey);
                            EncryptionState = EncryptionState.SymmetricalReady;
                            break;
                        case EncryptionFunction.AsymmetricalKeyReceive:
                            EncryptionState = EncryptionState.AsymmetricalReady;
                            //Log.Info("Server got my Asymmetrical key.");
                            break;
                        case EncryptionFunction.SymetricalKeyReceive:
                            Log.Error("Server should not be receiving my symmetrical key!");
                            break;
                        case EncryptionFunction.UpdateEncryptionStatus:
                            //Log.Info($"Server updated my encryption state: {encryptionPacket.State.ToString()}");
                            EncryptionState = encryptionPacket.State;
                            if (encryptionPacket.State == EncryptionState.Encrypted)
                            {
                                Log.Success("Encryption Successful.");
                            }
                            if (AuthenticationProvider != null && AuthenticationProvider.ClientInitiate)
                            {
                                AuthenticationPacket authenticationPacket2 = AuthenticationProvider.BeginAuthentication();
                                Send(authenticationPacket2);
                            }
                            break;
                        default:
                            Log.Error($"Invalid Encryption function: {encryptionPacket.EncryptionFunction}");
                            break;
                    }
                    break;
                default:
                    Log.Error($"Packet is not handled! Type: {header.Type}");
                    Disconnect("Server Sent an Unknown packet with PacketID " + header.Type.ToString());
                    break;
            }
        }

        protected void HandlePacket(PacketHeader header, byte[] fullPacket)
        {
            if (CurrentClientLocation == ClientLocation.Remote)
            {
                HandleRemoteClient(header, fullPacket);
            }
            if (CurrentClientLocation == ClientLocation.Local)
            {
                HandleLocalClient(header, fullPacket);
            }
        }

        /// <summary>
        /// Handles the next <see cref="Packet"/> from the read queue. This method does nothing if <see cref="ManualPacketHandle"/> is false
        /// </summary>
        public void HandleNextPacket()
        {
            if (!ManualPacketHandle)
            {
                return;
            }
            if (_toReadPackets.TryDequeue(out ReadPacketInfo result))
            {
                HandlePacket(result.Header, result.Data);
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// Reads the next packet and handles it. (Non-blocking)
        /// </summary>
        internal void ReadNext()
        {
            RawReader();
            //Log.Debug("Reader ran");
        }

        /// <summary>
        /// Writes the next packet. (Blocking)
        /// </summary>
        internal void WriteNext()
        {
            RawWriter();
            //Log.Debug("Writer ran");
        }

        #endregion

        #region Keep Alive / Latency


        long _latency = 0;

        DateTime _lastSent = DateTime.UtcNow;

        protected virtual void DoLatencyCheck()
        {
            if (DateTime.UtcNow - _lastSent >= TimeSpan.FromMilliseconds(MaxMSBeforeKeepAlive))
            {
                CheckLatency();
            }
        }

        protected virtual void DoLatency(KeepAlivePacket packet)
        {
            _latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - packet.SendTime;
            InvokeLatencyChanged(_latency);
        }

        /// <summary>
        /// Forces the current <see cref="NetworkClient"/> to send a <see cref="KeepAlivePacket"/> and measure latency.
        /// </summary>
        public virtual void CheckLatency()
        {
            KeepAlivePacket packet = new KeepAlivePacket();
            Send(packet);
            _lastSent = DateTime.UtcNow;
        }

        #endregion

        #region Misc

        /// <summary>
        /// Sends a log message to the other side of the <see cref="NetworkTransport"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="severity"></param>
        public void SendLog(string message, LogSeverity severity)
        {
            NetworkInvoke(nameof(GetError), new object[] { message, (int)severity });
        }

        [NetworkInvokable(NetworkDirection.Any)]
        private void GetError(NetworkHandle handle, string message, int level)
        {
            Log.Any("[From Peer]: " + message, (LogSeverity)level);
        }

        private void OnReadyStateChanged(bool oldState, bool newState)
        {
            if (!newState)
            {
                return;
            }
            ServerBeginSync();
            if (NetworkServer.ClientAvatar != null && NetworkServer.ClientAvatar.GetInterfaces().Contains(typeof(INetworkAvatar)))
            {
                INetworkObject result = null;
                NetworkObjectSpawner spawner = NetworkManager.GetBestSpawner(NetworkServer.ClientAvatar);
                if (spawner != null)
                {
                    result = (INetworkObject)spawner.Spawner.Invoke(null, new NetworkHandle(this));
                }
                else
                {
                    result = (INetworkObject)Activator.CreateInstance(NetworkServer.ClientAvatar);
                }
                if (result != null)
                {
                    ServerSpecifyAvatar((INetworkAvatar)result);
                }
            }
        }

        /// <summary>
        /// Forces the server to begin syncing <see cref="INetworkObject"/>s. This is done async, so your changed may or may not be accepted if you decide to change anything after this method is called. At least not by this <see cref="Task"/>.
        /// </summary>
        public Task ServerBeginSync()
        {
            return Task.Run(() =>
            {
                List<INetworkObject> objects = NetworkManager.GetNetworkObjects().Where(x => x.Spawnable).ToList();
                NetworkInvoke(nameof(OnSyncBegin), new object[] { objects.Count });
                foreach (INetworkObject @object in objects)
                {
                    @object.NetworkSpawn(this);
                    @object.OnSync(this);
                }
            });
        }

        [NetworkInvokable(Direction = NetworkDirection.Server)]
        private void OnSyncBegin(NetworkHandle handle, int objCount)
        {
            Log.Info("Total of Network Objects that will be spawned automatically: " + objCount);
        }

        /// <summary>
        /// Overrides an existing <see cref="INetworkAvatar"/> on the local and remote client. if the <see cref="INetworkAvatar"/> is not spawned, it will be. If <see cref="INetworkObject.ObjectVisibilityMode"/> (<see cref="INetworkObject"/> is the parent interface for <see cref="INetworkAvatar"/>) is <see cref="ObjectVisibilityMode.ServerOnly"/>, it will be reset to <see cref="ObjectVisibilityMode.Everyone"/>. The <see cref="INetworkObject.OwnershipMode"/> will be set to <see cref="OwnershipMode.Client"/> as well as the <see cref="INetworkObject.OwnerClientID"/>. All of these changes will not generate additional packets.
        /// </summary>
        /// <param name="avatar"></param>
        public void ServerSpecifyAvatar(INetworkAvatar avatar)
        {
            NetworkManager.AddNetworkObject(avatar);
            avatar.OwnerClientID = ClientID;
            avatar.OwnershipMode = OwnershipMode.Client;
            if (avatar.ObjectVisibilityMode == ObjectVisibilityMode.ServerOnly)
            {
                avatar.ObjectVisibilityMode = ObjectVisibilityMode.Everyone;
            }
            avatar.NetworkSpawn();
            NetworkInvoke(nameof(GetClientAvatar), new object[] { avatar.NetworkID });
            Log.Info($"Avatar Specify: {avatar.NetworkID}");
            _avatar = avatar;
        }

        [NetworkInvokable(Direction = NetworkDirection.Server)]
        private void GetClientAvatar(NetworkHandle handle, int id)
        {
            Log.Info("New Client avatar has been specified. ID: " + id);
            (INetworkObject, NetworkObjectData) result = NetworkManager.GetNetworkObjectByID(id);
            if (result.Item1 == null)
            {
                Log.Warning("Got a client avatar, can't find the ID? ID: " + id);
                return;
            }
            Avatar = (INetworkAvatar)result.Item1;
        }

        #endregion
    }

    public struct ReadPacketInfo
    {
        public PacketHeader Header;
        public byte[] Data;
    }

    public struct NetworkErrorData
    {
        public string Error { get; private set; }
        public bool IsConnected { get; private set; }

        public NetworkErrorData(string error, bool isConnected)
        {
            Error = error;
            IsConnected = isConnected;
        }

        public override string ToString()
        {
            return "Is Connected: " + IsConnected + " Error: " + Error;
        }
    }
}
