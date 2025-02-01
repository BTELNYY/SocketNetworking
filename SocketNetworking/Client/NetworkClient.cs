using SocketNetworking.Attributes;
using SocketNetworking.Misc;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking.Transports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace SocketNetworking.Client
{
    public class NetworkClient
    {

        static NetworkClient instance;

        /// <summary>
        /// Gets the local singleton for the <see cref="NetworkClient"/>. Can only be called on the client, multiple <see cref="NetworkClient"/>s on the local context are not allowed.
        /// </summary>
        public static NetworkClient LocalClient
        {
            get
            {
                if(NetworkManager.WhereAmI == ClientLocation.Remote)
                {
                    throw new InvalidOperationException("Cannot get local client from the server.");
                }
                return instance;
            }
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

        public NetworkClient()
        {
            Log = new Log()
            {
                Prefix = $"[Client (No ID)]"
            };
            ClientCreated?.Invoke(this);
            _networkEncryptionManager = new NetworkEncryptionManager();
            Init();
        }

        /// <summary>
        /// Called on the server and client when a client is created.
        /// </summary>
        public virtual void Init()
        {

        }


        #region Per Client (Non-Static) Events
        /// <summary>
        /// Called on both Remote and Local clients when the connection has succeeded and the Socket is ready to use.
        /// </summary>
        public event Action ClientConnected;

        /// <summary>
        /// Called on the both Remote and Local clients when the connection stops. Note that Remote Clients will be destroyed.
        /// </summary>
        public event Action ClientDisconnected;

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
        /// Called when a client is created, gives the <see cref="NetworkClient"/> that was created.
        /// </summary>
        public static event Action<NetworkClient> ClientCreated;

        #endregion

        #region Properties

        /// <summary>
        /// Provdies the <see cref="SocketNetworking.Log"/> instance for this client. The <see cref="Log.Prefix"/> is set to contain the client ID for logging purposes.
        /// </summary>
        public Log Log { get; }

        /// <summary>
        /// The Avatar of the <see cref="NetworkClient"/>. This can be specified in 
        /// </summary>
        public INetworkObject Avatar { get; private set; }

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

        protected NetworkEncryptionManager _networkEncryptionManager;

        public NetworkEncryptionManager EncryptionManager
        {
            get
            {
                return _networkEncryptionManager;
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
        public string ConnectedIP
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

        private bool _ready = false;

        /// <summary>
        /// Determines the ready state of the <see cref="NetworkClient"/>, this has no effect on library logic but can be useful for applications using the library.
        /// </summary>
        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                if(!IsTransportConnected || CurrentConnectionState != ConnectionState.Connected) 
                {
                    Log.Warning("Can't change ready state becuase the socket is not connected or the handshake isn't done.");
                    return;
                }
                ReadyStateUpdatePacket readyStateUpdatePacket = new ReadyStateUpdatePacket
                {
                    Ready = value
                };
                Send(readyStateUpdatePacket);
                if (CurrnetClientLocation == ClientLocation.Remote) 
                {
                    _ready = value;
                    ReadyStateChanged?.Invoke(!_ready, _ready);
                    ClientReadyStateChanged?.Invoke(this);
                    NetworkManager.SendReadyPulse(this, Ready);
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
                if(CurrentConnectionState == ConnectionState.Handshake)
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
                if(CurrentConnectionState == ConnectionState.Handshake)
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
                if(Transport == null)
                {
                    return false;
                }
                if(CurrnetClientLocation == ClientLocation.Remote && Transport.IsConnected)
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
                return CurrentConnectionState == ConnectionState.Connected || CurrentConnectionState == ConnectionState.Handshake;
            }
            set
            {
                if(IsConnected)
                {
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// <see cref="bool"/> which represents if the Client has been started
        /// </summary>
        private bool ClientStarted => CurrnetClientLocation == ClientLocation.Remote || _clientActive;

        private ConnectionState _connectionState = ConnectionState.Disconnected;

        /// <summary>
        /// The <see cref="ConnectionState"/> of the current client. Can only be set by clients which have the <see cref="ClientLocation.Remote"/> <see cref="CurrnetClientLocation"/>
        /// </summary>
        public ConnectionState CurrentConnectionState
        {
            get => _connectionState;
            set
            {
                if(CurrnetClientLocation != ClientLocation.Remote)
                {
                    Log.Error("Local client tried changing state of connection, only servers can do so.");
                    return;
                }
                ConnectionUpdatePacket updatePacket = new ConnectionUpdatePacket
                {
                    State = value,
                    Reason = "Setter in remote."
                };
                Send(updatePacket);
                _connectionState = value;
                ConnectionStateUpdated?.Invoke(value);
                ClientConnectionStateChanged?.Invoke(this);
                NetworkManager.SendConnectedPulse(this);
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
                Log.Debug($"Encryption State Updated: {_encryptionState}, As Number: {(int)_encryptionState}");
                if(value >= EncryptionState.SymmetricalReady)
                {
                    EncryptionComplete?.Invoke();
                }
            }
        }

        private bool _clientActive = false;

        private string _clientPassword = "DefaultPassword";

        public string PasswordHash => _clientPassword.GetStringHash();

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
        public ClientLocation CurrnetClientLocation
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
            return NetworkInvoke<bool>(nameof(ServerGetEncryptionRequest), new object[] { });
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
            if (CurrnetClientLocation != ClientLocation.Remote || EncryptionState > EncryptionState.Disabled)
            {
                return;
            }
            EncryptionPacket packet = new EncryptionPacket();
            packet.EncryptionFunction = EncryptionFunction.AsymmetricalKeySend;
            packet.PublicKey = EncryptionManager.MyPublicKey;
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

        #endregion

        #region Init

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
            ClientConnected += OnRemoteClientConnected;
            ClientConnected?.Invoke();
            ReadyStateChanged += OnReadyStateChanged;
            //_packetReaderThread = new Thread(PacketReaderThreadMethod);
            //_packetReaderThread.Start();
            //_packetSenderThread = new Thread(PacketSenderThreadMethod);
            //_packetSenderThread.Start();
        }

        private void OnReadyStateChanged(bool oldState, bool newState)
        {
            if(!newState)
            {
                return;
            }
            List<INetworkObject> objects = NetworkManager.GetNetworkObjects().Where(x => x.Spawnable).ToList();
            NetworkInvoke(nameof(OnSyncBegin), new object[] { objects.Count });
            foreach(INetworkObject @object in objects)
            {
                @object.OnSync(this);
                @object.NetworkSpawn(this);
            }
            if(NetworkServer.ClientAvatar != null && NetworkServer.ClientAvatar.GetInterfaces().Contains(typeof(INetworkObject)))
            {
                INetworkObject result = null;
                NetworkObjectSpawner spawner = NetworkManager.GetBestSpawner(NetworkServer.ClientAvatar);
                if(spawner != null)
                {
                    result = (INetworkObject)spawner.Spawner.Invoke(null, new NetworkHandle(this));
                }
                else
                {
                    result = (INetworkObject)Activator.CreateInstance(NetworkServer.ClientAvatar);
                }
                if(result != null)
                {
                    result.OwnerClientID = ClientID;
                    result.OwnershipMode = OwnershipMode.Client;
                    result.ObjectVisibilityMode = ObjectVisibilityMode.Everyone;
                    NetworkManager.AddNetworkObject(result);
                    result.NetworkSpawn();
                    NetworkInvoke(nameof(GetClientAvatar), new object[] { result.NetworkID });
                }
            }
        }

        [NetworkInvokable(Direction = NetworkDirection.Server)]
        private void OnSyncBegin(NetworkHandle handle, int objCount)
        {
            Log.Info("Total of Network Objects that will be spawned automatically: " + objCount);
        }

        [NetworkInvokable(Direction = NetworkDirection.Server)]
        private void GetClientAvatar(NetworkHandle handle, int id)
        {
            Log.Info("New Client avatar has been specified. ID: " + id);
            var result = NetworkManager.GetNetworkObjectByID(id);
            if(result.Item1 == null)
            {
                Log.Warning("Got a client avatar, can't find the ID? ID: " + id);
                return;
            }
            Avatar = result.Item1;
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
        /// An <see cref="int"/> representation of the port
        /// </param>
        /// <param name="password">
        /// A <see cref="string"/> representing the password. Note that it will be hashed when sending to the server.
        /// </param>
        /// <returns>
        /// A <see cref="bool"/> indicating connection success. Note this only returns the status of the socket connection, not of the full connection action. E.g. you can still fail to connect if the server refuses to accept the client.
        /// </returns>
        public bool Connect(string hostname, int port, string password)
        {
            if(CurrnetClientLocation == ClientLocation.Remote)
            {
                Log.Error("Cannot connect to other servers from remote.");
                return false;
            }
            if (IsTransportConnected)
            {
                Log.Error("Can't connect: Already connected to a server.");
                return false;
            }
            try
            {
                Exception ex = Transport.Connect(hostname, port);
                if (ex != null)
                {
                    throw ex;
                }
            }
            catch(Exception ex)
            {
                NetworkErrorData networkErrorData = new NetworkErrorData("Connection Failed: " + ex.ToString(), false);
                ConnectionError?.Invoke(networkErrorData);
                Log.Error($"Failed to connect: \n {ex}");
                return false;
            }
            _clientPassword = password;
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
            if (!IsConnected || !IsTransportConnected)
            {
                return;
            }
            ConnectionUpdatePacket connectionUpdatePacket = new ConnectionUpdatePacket
            {
                State = ConnectionState.Disconnected,
                Reason = message
            };
            Send(connectionUpdatePacket);
            _connectionState = ConnectionState.Disconnected;
            NetworkErrorData errorData = new NetworkErrorData("Disconnected. Reason: " + connectionUpdatePacket.Reason, false);
            ConnectionError?.Invoke(errorData);
            ClientDisconnected?.Invoke();
            if (CurrnetClientLocation == ClientLocation.Remote)
            {
                Log.Info($"Disconnecting Client {ClientID} for " + message);
                StopClient();
            }
            if (CurrnetClientLocation == ClientLocation.Local)
            {
                Log.Info("Disconnecting from server. Reason: " + message);
                StopClient();
            }
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
            NetworkManager.SendDisconnectedPulse(this);
            if (CurrnetClientLocation == ClientLocation.Remote)
            {
                NetworkServer.RemoveClient(ClientID);
            }
            _connectionState = ConnectionState.Disconnected;
            Transport?.Close();
            _shuttingDown = true;
            //_packetReaderThread?.Abort();
            //_packetSenderThread?.Abort();
            //_packetReaderThread = null;
            //_packetSenderThread = null;
            if (Clients.Contains(this))
            {
                Clients.Remove(this);
            }
        }

        void StartClient()
        {
            if (CurrnetClientLocation == ClientLocation.Remote)
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
        [Obsolete("This method is not thread safe. Use Send(Packet) instead.")]
        public void SendImmediate(Packet packet)
        {
            PreparePacket(ref packet);
            byte[] fullBytes = SerializePacket(packet);
            try
            {
                Log.Debug($"Sending packet. Target: {packet.NetowrkIDTarget} Type: {packet.Type} CustomID: {packet.CustomPacketID} Length: {fullBytes.Length}");
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
        public void Send(Packet packet, INetworkObject sender)
        {
            packet.NetowrkIDTarget = sender.NetworkID;
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
        public void Send(Packet packet, INetworkObject sender, bool priority)
        {
            packet.NetowrkIDTarget = sender.NetworkID;
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
            return NetworkManager.NetworkInvoke<T>(target, this, methodName, args, maxTimeMs, priority);
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
        /// Preforms a blocking Network Invocation (Like an RPC) and attempts to return you a value. This will try to find the method on the current <see cref="NetworkClient"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="maxTimeMs"></param>
        /// <returns></returns>
        public T NetworkInvoke<T>(string methodName, object[] args, float maxTimeMs = 5000, bool priority = false)
        {
            return NetworkManager.NetworkInvoke<T>(this, this, methodName, args, maxTimeMs, priority);
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

        #endregion

        #region Internal Events

        void OnLocalClientConnected()
        {
            if (CurrnetClientLocation != ClientLocation.Local)
            {
                return;
            }
            ClientDataPacket dataPacket = new ClientDataPacket(_clientPassword);
            Send(dataPacket);
        }

        void OnRemoteClientConnected()
        {
            if (CurrnetClientLocation != ClientLocation.Remote)
            {
                return;
            }
            CurrentConnectionState = ConnectionState.Handshake;
        }

        #endregion

        #region Sending/Recieving

        #region Sending

        protected object streamLock = new object();

        protected ConcurrentQueue<Packet> _toSendPackets = new ConcurrentQueue<Packet>();

        protected virtual void SendNextPacketInternal()
        {
            if (_toSendPackets.IsEmpty)
            {
                return;
            }
            lock (streamLock)
            {
                _toSendPackets.TryDequeue(out Packet packet);
                PreparePacket(ref packet);
                byte[] fullBytes = SerializePacket(packet);
                try
                {
                    Log.Debug($"Sending packet. Target: {packet.NetowrkIDTarget} Type: {packet.Type} CustomID: {packet.CustomPacketID} Length: {fullBytes.Length}");
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
            }
        }

        /// <summary>
        /// Called before serialization, if the base is not called, the <see cref="Packet.Source"/> will be null.
        /// </summary>
        /// <param name="packet"></param>
        protected virtual void PreparePacket(ref Packet packet)
        {
            packet.Source = Transport.LocalEndPoint;
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
                //Ensure the packet isnt ecnrypted if we don't support it.
                //Log.Debug("Encryption is not supported at this moment, ensuring it isn't flagged as being enabled on this packet.");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.AsymetricalEncrypted, false);
                packet.Flags = packet.Flags.SetFlag(PacketFlags.SymetricalEncrypted, false);
            }
            if (!packet.ValidateFlags())
            {
                Log.Error($"Packet send failed! Flag validation failure. Packet Type: {packet.Type}, Target: {packet.NetowrkIDTarget}, Custom Packet ID: {packet.CustomPacketID}, Active Flags: {string.Join(", ", packet.Flags.GetActiveFlags())}");
                return null;
            }
            Log.Debug("Active Flags: " + string.Join(", ", packet.Flags.GetActiveFlags()));
            byte[] packetBytes = packet.Serialize().Data;
            byte[] packetHeaderBytes = packetBytes.Take(PacketHeader.HeaderLength - 4).ToArray();
            byte[] packetDataBytes = packetBytes.Skip(PacketHeader.HeaderLength - 4).ToArray();
            //StringBuilder hex = new StringBuilder(packetBytes.Length * 2);
            //foreach (byte b in packetBytes)
            //{
            //    hex.AppendFormat("{0:x2}", b);
            //}
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
                if(packetDataBytes.Length > NetworkEncryptionManager.MaxBytesForAsym)
                {
                    Log.Warning($"Packet is too large for RSA! Packet Size: {packetDataBytes.Length}, Max Packet Size: {NetworkEncryptionManager.MaxBytesForAsym}");
                }
                else
                {
                    Log.Debug("Encrypting Packet: Asymmetrical");
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
                Log.Debug("Encrypting Packet: Symmetrical");
                packetDataBytes = EncryptionManager.Encrypt(packetDataBytes);
                if(packetDataBytes.Length == 0)
                {
                    Log.Error("Encryption resulted in a null!");
                    return null;
                }
            }
            ByteWriter writer = new ByteWriter();
            byte[] packetFull = packetHeaderBytes.Concat(packetDataBytes).ToArray();
            Log.Debug($"Packet Size: Full (Raw): {packetBytes.Length}, Full (Processed): {packetFull.Length}. With Header Size: {packetFull.Length + 4}");
            writer.WriteInt(packetFull.Length);
            writer.Write(packetFull);
            int written = writer.DataLength;
            if(written != (packetFull.Length + 4))
            {
                Log.Error($"Trying to send corrupted size! Custom Packet ID: {packet.CustomPacketID}, Target: {packet.NetowrkIDTarget}, Size: {written}, Expected: {packetFull.Length + 4}");
                return null;
            }
            byte[] fullBytes = writer.Data;
            if (fullBytes.Length > Packet.MaxPacketSize)
            {
                Log.Error("Packet too large!");
                return null;
            }
            //StringBuilder hex1 = new StringBuilder(fullBytes.Length * 2);
            //foreach (byte b in fullBytes)
            //{
            //    hex1.AppendFormat("{0:x2}", b);
            //}
            //Log.Debug("Full Packet: \n" + hex1.ToString());
            return fullBytes;
        }

        void PacketSenderThreadMethod()
        {
            while (true)
            {
                RawWriter();
            }
        }

        protected virtual void RawWriter()
        {
            if (_manualPacketSend)
            {
                return;
            }
            SendNextPacketInternal();
        }

        #endregion

        #region Recieving

        protected ConcurrentQueue<ReadPacketInfo> _toReadPackets = new ConcurrentQueue<ReadPacketInfo>();

        /// <summary>
        /// Method handling all <see cref="NetworkTransport"/> reading IO.
        /// </summary>
        protected virtual void PacketReaderThreadMethod()
        {
            Log.Info($"Client thread started, ID {ClientID}");
            while (true)
            {
                if (_shuttingDown)
                {
                    Log.Info("Shutting down loop");
                    break;
                }
                RawReader();
            }
            Log.Info("Shutting down client, Closing socket.");
            Transport.Close();
        }

        /// <summary>
        /// Method which reads actual data and proccesses it from the Network I/O, this is a blocking, single read method, it will not attempt to keep reading if there is not data on the <see cref="Transport"/>.
        /// </summary>
        protected virtual void RawReader()
        {
            if (!IsTransportConnected)
            {
                StopClient();
                return;
            }
            if (!Transport.DataAvailable)
            {
                return;
            }
            (byte[], Exception, IPEndPoint) packet = Transport.Receive();
            if (packet.Item1 == null)
            {
                Log.Warning("Transport recieved a null byte array.");
                return;
            }
            Deserialize(packet.Item1, packet.Item3);
            //if(!Transport.DataAvailable)
            //{
            //    //Log.Debug("No data available.");
            //    return;
            //}
            //byte[] buffer = new byte[Packet.MaxPacketSize]; // this can now be freely changed
            //Transport.BufferSize = Packet.MaxPacketSize;
            //int fillSize = 0; // the amount of bytes in the buffer. Reading anything from fillsize on from the buffer is undefined.
            //// this is for breaking a nested loop further down. thanks C#
            //if (!IsTransportConnected)
            //{
            //    Log.Debug("Disconnected!");
            //    StopClient();
            //    return;
            //}
            ///*if(TcpClient.ReceiveBufferSize == 0)
            //{
            //    continue;
            //}*/
            ///*if (!NetworkStream.DataAvailable)
            //{
            //    //Log.Debug("Nothing to read on stream");
            //    continue;
            //}*/
            ////Log.Debug(TcpClient.ReceiveBufferSize.ToString());
            //if (fillSize < sizeof(int))
            //{
            //    // we dont have enough data to read the length data
            //    //Log.Debug($"Trying to read bytes to get length (we need at least 4 we have {fillSize})!");
            //    int count = 0;
            //    try
            //    {
            //        int tempFillSize = fillSize;
            //        //(byte[], Exception) transportRead = Transport.Receive(fillSize, buffer.Length - fillSize);
            //        (byte[], Exception, IPEndPoint) transportRead = Transport.Receive(0, buffer.Length - fillSize);
            //        count = transportRead.Item1.Length;
            //        buffer = Transport.Buffer;
            //        //count = NetworkStream.Read(tempBuffer, 0, buffer.Length - fillSize);
            //    }
            //    catch (Exception ex)
            //    {
            //        Log.Error(ex.ToString());
            //        return;
            //    }
            //    fillSize += count;
            //    //Log.Debug($"Read {count} bytes from buffer ({fillSize})!");
            //    return;
            //}
            //int bodySize = BitConverter.ToInt32(buffer, 0); // i sure do hope this doesnt modify the buffer.
            //bodySize = IPAddress.NetworkToHostOrder(bodySize);
            //if (bodySize == 0)
            //{
            //    Log.Warning("Got a malformed packet, Body Size can't be 0, Resetting header to beginning of Packet (may cuase duplicate packets)");
            //    fillSize = 0;
            //    return;
            //}
            //fillSize -= sizeof(int); // this kinda desyncs fillsize from the actual size of the buffer, but eh
            //                         // read the rest of the whole packet
            //if (bodySize > Packet.MaxPacketSize || bodySize < 0)
            //{
            //    CurrentConnectionState = ConnectionState.Disconnected;
            //    string s = string.Empty;
            //    for (int i = 0; i < buffer.Length; i++)
            //    {
            //        s += Convert.ToString(buffer[i], 2).PadLeft(8, '0') + " ";
            //    }
            //    Log.Error("Body Size is corrupted! Raw: " + s);
            //}
            //while (fillSize < bodySize)
            //{
            //    //Log.Debug($"Trying to read bytes to read the body (we need at least {bodySize} and we have {fillSize})!");
            //    if (fillSize == buffer.Length)
            //    {
            //        // The buffer is too full, and we are fucked (oh shit)
            //        Log.Error("Buffer became full before being able to read an entire packet. This probably means a packet was sent that was bigger then the buffer (Which is the packet max size). This is not recoverable, Disconnecting!");
            //        Disconnect("Illegal Packet Size");
            //        break;
            //    }
            //    int count;
            //    try
            //    {
            //        (byte[], Exception, IPEndPoint) transportRead = Transport.Receive(fillSize, buffer.Length - fillSize);
            //        count = transportRead.Item1.Length;
            //        buffer = Transport.Buffer;
            //        //count = NetworkStream.Read(buffer, fillSize, buffer.Length - fillSize);
            //    }
            //    catch (Exception ex)
            //    {
            //        Log.Error(ex.ToString());
            //        return;
            //    }
            //    fillSize += count;
            //}
            //// we now know we have enough bytes to read at least one whole packet;
            //byte[] fullPacket = ShiftOut(ref buffer, bodySize + sizeof(int));
            //if ((fillSize -= bodySize) < 0)
            //{
            //    fillSize = 0;
            //}
            ////fillSize -= bodySize; // this resyncs fillsize with the fullness of the buffer
            ////Log.Debug($"Read full packet with size: {fullPacket.Length}");
            //Deserialize(fullPacket, Transport.Peer);
        }

        /// <summary>
        /// Called when deserializing the packet, preforms all functions needed to get the proper packet object and queue it for processing.
        /// </summary>
        /// <param name="fullPacket"></param>
        /// <param name="endpoint"></param>
        protected virtual void Deserialize(byte[] fullPacket, IPEndPoint endpoint)
        {
            //StringBuilder hex = new StringBuilder(fullPacket.Length * 2);
            //foreach (byte b in fullPacket)
            //{
            //    hex.AppendFormat("{0:x2}", b);
            //}
            //Log.Debug(hex.ToString());
            PacketHeader header = Packet.ReadPacketHeader(fullPacket);
            if (header.Type == PacketType.CustomPacket && NetworkManager.GetCustomPacketByID(header.CustomPacketID) == null)
            {
                Log.Warning($"Got a packet with a Custom Packet ID that does not exist, either not registered or corrupt. Custom Packet ID: {header.CustomPacketID}, Target: {header.NetworkIDTarget}");
            }
            Log.Debug("Active Flags: " + string.Join(", ", header.Flags.GetActiveFlags()));
            Log.Debug($"Inbound Packet Info, Size Of Full Packet: {header.Size}, Type: {header.Type}, Target: {header.NetworkIDTarget}, CustomPacketID: {header.CustomPacketID}");
            byte[] rawPacket = fullPacket;
            byte[] headerBytes = fullPacket.Take(PacketHeader.HeaderLength).ToArray();
            byte[] packetBytes = fullPacket.Skip(PacketHeader.HeaderLength).ToArray();
            int currentEncryptionState = (int)EncryptionState;
            if (header.Flags.HasFlag(PacketFlags.SymetricalEncrypted))
            {
                Log.Debug("Trying to decrypt a packet using SYMMETRICAL encryption!");
                if (currentEncryptionState < (int)EncryptionState.SymmetricalReady)
                {
                    Log.Error("Encryption cannot be done at this point: Not ready.");
                    return;
                }
                packetBytes = EncryptionManager.Decrypt(packetBytes);
            }
            if (header.Flags.HasFlag(PacketFlags.AsymetricalEncrypted))
            {
                Log.Debug("Trying to decrypt a packet using ASYMMETRICAL encryption!");
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
            //foreach (byte b in fullPacket)
            //{
            //    hex1.AppendFormat("{0:x2}", b);
            //}
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
                case PacketType.CustomPacket:
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
                    if ((clientDataPacket.PasswordHash != NetworkServer.Config.ServerPassword.GetStringHash()) && NetworkServer.Config.UseServerPassword)
                    {
                        Disconnect("Incorrect Server Password");
                        break;
                    }
                    ServerDataPacket serverDataPacket = new ServerDataPacket
                    {
                        YourClientID = _clientId,
                        Configuration = NetworkServer.ServerConfiguration,
                        CustomPacketAutoPairs = NetworkManager.PacketPairsSerialized
                    };
                    Send(serverDataPacket);
                    CurrentConnectionState = ConnectionState.Connected;
                    ClientIdUpdated?.Invoke();
                    if (NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Required)
                    {
                        ServerBeginEncryption();
                    }
                    else if (NetworkServer.Config.DefaultReady && NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Disabled)
                    {
                        Ready = true;
                    }
                    break;
                case PacketType.NetworkInvocation:
                    NetworkInvokationPacket networkInvocationPacket = new NetworkInvokationPacket();
                    networkInvocationPacket.Deserialize(data);
                    Log.Debug($"Network Invocation: ObjectID: {networkInvocationPacket.NetworkObjectTarget}, Method: {networkInvocationPacket.MethodName}, Arguments Count: {networkInvocationPacket.Arguments.Count}");
                    try
                    {
                        NetworkManager.NetworkInvoke(networkInvocationPacket, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Network Invocation Failed! Method {networkInvocationPacket.MethodName}, Error: {ex}");
                        NetworkInvokationResultPacket errorPacket = new NetworkInvokationResultPacket();
                        errorPacket.Success = false;
                        errorPacket.ErrorMessage = $"Method: {networkInvocationPacket.MethodName} Message: " + ex.Message;
                        errorPacket.Result = SerializedData.NullData;
                        errorPacket.CallbackID = networkInvocationPacket.CallbackID;
                        errorPacket.IgnoreResult = false;
                        Send(errorPacket);
                    }
                    break;
                case PacketType.NetworkInvocationResult:
                    NetworkInvokationResultPacket networkInvocationResultPacket = new NetworkInvokationResultPacket();
                    networkInvocationResultPacket.Deserialize(data);
                    Log.Debug($"NetworkInvocationResult: CallbackID: {networkInvocationResultPacket.CallbackID}, Success?: {networkInvocationResultPacket.Success}, Error Message: {networkInvocationResultPacket.ErrorMessage}");
                    NetworkManager.NetworkInvoke(networkInvocationResultPacket, this);
                    break;
                case PacketType.EncryptionPacket:
                    EncryptionPacket encryptionPacket = new EncryptionPacket();
                    encryptionPacket.Deserialize(data);
                    Log.Info($"Encryption request! Function {encryptionPacket.EncryptionFunction}");
                    switch (encryptionPacket.EncryptionFunction)
                    {
                        case EncryptionFunction.None:
                            break;
                        case EncryptionFunction.AsymmetricalKeySend:
                            EncryptionManager.OthersPublicKey = encryptionPacket.PublicKey;
                            EncryptionPacket gotYourPublicKey = new EncryptionPacket();
                            gotYourPublicKey.EncryptionFunction = EncryptionFunction.AsymmetricalKeyRecieve;
                            Send(gotYourPublicKey);
                            EncryptionState = EncryptionState.AsymmetricalReady;
                            Log.Info($"Got Asymmetrical Encryption Key, ID: {ClientID}");
                            EncryptionPacket sendSymKey = new EncryptionPacket();
                            sendSymKey.EncryptionFunction = EncryptionFunction.SymmetricalKeySend;
                            sendSymKey.SymKey = EncryptionManager.SharedAesKey.Item1;
                            sendSymKey.SymIV = EncryptionManager.SharedAesKey.Item2;
                            Send(sendSymKey);
                            break;
                        case EncryptionFunction.SymmetricalKeySend:
                            Disconnect("Illegal encryption handshake: Cannot send own symmetry key, wait for server.");
                            break;
                        case EncryptionFunction.AsymmetricalKeyRecieve:
                            Log.Info($"Client Got Asymmetrical Encryption Key, ID: {ClientID}");
                            break;
                        case EncryptionFunction.SymetricalKeyRecieve:
                            EncryptionState = EncryptionState.SymmetricalReady;
                            Log.Info($"Client Got Symmetrical Encryption Key, ID: {ClientID}");
                            EncryptionPacket updateEncryptionStateFinal = new EncryptionPacket();
                            updateEncryptionStateFinal.EncryptionFunction = EncryptionFunction.UpdateEncryptionStatus;
                            updateEncryptionStateFinal.State = EncryptionState.Encrypted;
                            Send(updateEncryptionStateFinal);
                            if(NetworkServer.Config.DefaultReady == true)
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
                    Log.Error($"Packet is not handled! Info: Target: {header.NetworkIDTarget}, Type Provided: {header.Type}, Size: {header.Size}, Custom Packet ID: {header.CustomPacketID}");
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
                case PacketType.CustomPacket:
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
                    catch(Exception ex)
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
                    catch(Exception ex)
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
                    if (serverDataPacket.Configuration.Protocol != ClientConfiguration.Protocol || serverDataPacket.Configuration.Version != ClientConfiguration.Version)
                    {
                        Disconnect($"Server protocol mismatch. Expected: {ClientConfiguration} Got: {serverDataPacket.Configuration}");
                        break;
                    }
                    ClientIdUpdated?.Invoke();
                    Dictionary<int, string> NewPacketPairs = serverDataPacket.CustomPacketAutoPairs;
                    List<Type> homelessPackets = new List<Type>();
                    foreach (int i in NewPacketPairs.Keys)
                    {
                        Type t = Type.GetType(NewPacketPairs[i]);
                        if (t == null)
                        {
                            Log.Error($"Can't find packet with fullname {NewPacketPairs[i]}, this will cause more errors later!");
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
                        built += $"ID: {i}, Fullname: {NetworkManager.AdditionalPacketTypes[i].FullName}\n";
                    }
                    Log.Info("Finished re-writing dynamic packets: " + built);
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
                    Log.Debug($"Network Invocation: ObjectID: {networkInvocationPacket.NetworkObjectTarget}, Method: {networkInvocationPacket.MethodName}, Arguments Count: {networkInvocationPacket.Arguments.Count}");
                    try
                    {
                        NetworkManager.NetworkInvoke(networkInvocationPacket, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Network Invocation Failed! Method {networkInvocationPacket.MethodName}, Error: {ex}");
                        NetworkInvokationResultPacket errorPacket = new NetworkInvokationResultPacket();
                        errorPacket.Success = false;
                        errorPacket.ErrorMessage = $"Method: {networkInvocationPacket.MethodName} Message: " + ex.Message;
                        errorPacket.Result = SerializedData.NullData;
                        errorPacket.CallbackID = networkInvocationPacket.CallbackID;
                        errorPacket.IgnoreResult = false;
                        Send(errorPacket);
                    }
                    break;
                case PacketType.NetworkInvocationResult:
                    NetworkInvokationResultPacket networkInvocationResultPacket = new NetworkInvokationResultPacket();
                    networkInvocationResultPacket.Deserialize(data);
                    Log.Debug($"NetworkInvocationResult: CallbackID: {networkInvocationResultPacket.CallbackID}, Success?: {networkInvocationResultPacket.Success}, Error Message: {networkInvocationResultPacket.ErrorMessage}");
                    NetworkManager.NetworkInvoke(networkInvocationResultPacket, this);
                    break;
                case PacketType.EncryptionPacket:
                    EncryptionPacket encryptionPacket = new EncryptionPacket();
                    encryptionPacket.Deserialize(data);
                    EncryptionPacket encryptionRecieve = new EncryptionPacket();
                    Log.Info($"Encryption request! Function {encryptionPacket.EncryptionFunction}");
                    switch (encryptionPacket.EncryptionFunction)
                    {
                        case EncryptionFunction.None:
                            break;
                        case EncryptionFunction.AsymmetricalKeySend:
                            EncryptionManager.OthersPublicKey = encryptionPacket.PublicKey;
                            EncryptionPacket gotYourPublicKey = new EncryptionPacket();
                            gotYourPublicKey.EncryptionFunction = EncryptionFunction.AsymmetricalKeyRecieve;
                            Log.Info("Got Servers Public key, Sending mine.");
                            Send(gotYourPublicKey);
                            EncryptionState = EncryptionState.AsymmetricalReady;
                            encryptionRecieve.PublicKey = EncryptionManager.MyPublicKey;
                            encryptionRecieve.EncryptionFunction = EncryptionFunction.AsymmetricalKeySend;
                            Send(encryptionRecieve);
                            break;
                        case EncryptionFunction.SymmetricalKeySend:
                            Log.Info($"Got servers symetrical key. Key: {string.Join("-", encryptionPacket.SymKey)}, IV: {string.Join("-", encryptionPacket.SymIV)}");
                            EncryptionManager.SharedAesKey = new Tuple<byte[], byte[]>(encryptionPacket.SymKey, encryptionPacket.SymIV);
                            EncryptionPacket gotYourSymmetricalKey = new EncryptionPacket();
                            gotYourSymmetricalKey.EncryptionFunction = EncryptionFunction.SymetricalKeyRecieve;
                            Send(gotYourSymmetricalKey);
                            EncryptionState = EncryptionState.SymmetricalReady;
                            break;
                        case EncryptionFunction.AsymmetricalKeyRecieve:
                            EncryptionState = EncryptionState.AsymmetricalReady;
                            Log.Info("Server got my Asymmetrical key.");
                            break;
                        case EncryptionFunction.SymetricalKeyRecieve:
                            Log.Error("Server should not be recieving my symmetrical key!");
                            break;
                        case EncryptionFunction.UpdateEncryptionStatus:
                            Log.Info($"Server updated my encryption state: {encryptionPacket.State.ToString()}");
                            EncryptionState = encryptionPacket.State;
                            break;
                        default:
                            Log.Error($"Invalid Encryption function: {encryptionPacket.EncryptionFunction}");
                            break;
                    }
                    break;
                default:
                    Log.Error($"Packet is not handled! Info: Target: {header.NetworkIDTarget}, Type Provided: {header.Type}, Size: {header.Size}, Custom Packet ID: {header.CustomPacketID}");
                    Disconnect("Server Sent an Unknown packet with PacketID " + header.Type.ToString());
                    break;
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

        protected void HandlePacket(PacketHeader header, byte[] fullPacket)
        {
            if (CurrnetClientLocation == ClientLocation.Remote)
            {
                HandleRemoteClient(header, fullPacket);
            }
            if (CurrnetClientLocation == ClientLocation.Local)
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

        /// <summary>
        /// Sends a log message to the other side of the <see cref="NetworkTransport"/>.
        /// </summary>
        /// <param name="error"></param>
        /// <param name="severity"></param>
        public void SendLog(string error, LogSeverity severity)
        {
            NetworkInvoke(nameof(GetError), new object[] { error, severity });
        }

        [NetworkInvokable(NetworkDirection.Any)]
        private void GetError(NetworkHandle handle, string err, LogSeverity level)
        {
            Log.Any(err, level);
        }

        public struct ReadPacketInfo
        {
            public PacketHeader Header;
            public byte[] Data;
        }



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
