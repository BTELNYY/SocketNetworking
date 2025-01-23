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
                    Log.GlobalWarning("Can't change ready state becuase the socket is not connected or the handshake isn't done.");
                    return;
                }
                if(CurrnetClientLocation == ClientLocation.Remote) 
                {
                    _ready = value;
                    ReadyStateChanged?.Invoke(!_ready, _ready);
                    ClientReadyStateChanged?.Invoke(this);
                    NetworkManager.SendReadyPulse(this, Ready);
                }
                ReadyStateUpdatePacket readyStateUpdatePacket = new ReadyStateUpdatePacket
                {
                    Ready = value
                };
                Send(readyStateUpdatePacket);
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
                    Log.GlobalWarning("Changing Packet read mode while the handshake has not yet finished, this may cause issues!");
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
                    Log.GlobalWarning("Changing Packet write mode while in handshake, things may break!");
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
                    Log.GlobalError("Local client tried changing state of connection, only servers can do so.");
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
                Log.GlobalDebug($"Encryption State Updated: {_encryptionState}, As Number: {(int)_encryptionState}");
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
                    Log.GlobalError("Can't update NetworkConfiguration while client is connected.");
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

        [NetworkInvocable(NetworkDirection.Client)]
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
            Transport = socket;
            _clientLocation = ClientLocation.Remote;
            ClientConnected += OnRemoteClientConnected;
            ClientConnected?.Invoke();
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
                Log.GlobalError("Cannot connect to other servers from remote.");
                return false;
            }
            if (IsTransportConnected)
            {
                Log.GlobalError("Can't connect: Already connected to a server.");
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
                Log.GlobalError($"Failed to connect: \n {ex}");
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
            SendImmediate(connectionUpdatePacket);
            _connectionState = ConnectionState.Disconnected;
            NetworkErrorData errorData = new NetworkErrorData("Disconnected. Reason: " + connectionUpdatePacket.Reason, false);
            ConnectionError?.Invoke(errorData);
            ClientDisconnected?.Invoke();
            if (CurrnetClientLocation == ClientLocation.Remote)
            {
                Log.GlobalInfo($"Disconnecting Client {ClientID} for " + message);
                StopClient();
            }
            if (CurrnetClientLocation == ClientLocation.Local)
            {
                Log.GlobalInfo("Disconnecting from server. Reason: " + message);
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
                Log.GlobalError("Can't start client on remote, started by constructor.");
                return;
            }
            if (ClientStarted)
            {
                Log.GlobalError("Can't start client, already started.");
                return;
            }
            Log.GlobalInfo("Starting client!");
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
        /// Forces the library to send the provided packet immediately.
        /// </summary>
        /// <param name="packet"></param>
        public void SendImmediate(Packet packet)
        {
            PreparePacket(ref packet);
            byte[] fullBytes = SerializePacket(packet);
            try
            {
                Log.GlobalDebug($"Sending packet. Target: {packet.NetowrkIDTarget} Type: {packet.Type} CustomID: {packet.CustomPacketID} Length: {fullBytes.Length}");
                Exception ex = Transport.Send(fullBytes, packet.Destination);
                if (ex != null)
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                Log.GlobalError("Failed to send packet! Error:\n" + ex.ToString());
                NetworkErrorData networkErrorData = new NetworkErrorData("Failed to send packet: " + ex.ToString(), true);
                ConnectionError?.Invoke(networkErrorData);
            }
        }

        /// <summary>
        /// Sends any <see cref="Packet"/> down the network, If you don't specify a <see cref="Packet.Destination"/>, the packet will be sent to <see cref="NetworkTransport.Peer"/> as set by <see cref="Transport"/>
        /// Note that this method doesn't check who it is sending it to, instead sending it to the current stream.
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> to send down the stream.
        /// </param>
        public void Send(Packet packet)
        {
            if (!IsTransportConnected)
            {
                Log.GlobalWarning("Can't Send packet, not connected!");
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
        /// Overwrites the NetworkTarget of the given packet to the ID from <see cref="INetworkObject"/>
        /// </summary>
        /// <param name="packet">
        /// Packet to overwrite
        /// </param>
        /// <param name="sender">
        /// ID to write
        /// </param>
        public void Send(Packet packet, INetworkObject sender)
        {
            packet.NetowrkIDTarget = sender.NetworkID;
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
        public T NetworkInvoke<T>(object target, string methodName, object[] args, float maxTimeMs = 5000)
        {
            return NetworkManager.NetworkInvoke<T>(target, this, methodName, args, maxTimeMs);
        }

        /// <summary>
        /// Preforms a non-blocking Network Invocation (Like an RPC)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public void NetworkInvoke(object target, string methodName, object[] args)
        {
            NetworkManager.NetworkInvoke(target, this, methodName, args);
        }

        /// <summary>
        /// Preforms a blocking Network Invocation (Like an RPC) and attempts to return you a value. This will try to find the method on the current <see cref="NetworkClient"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="maxTimeMs"></param>
        /// <returns></returns>
        public T NetworkInvoke<T>(string methodName, object[] args, float maxTimeMs = 5000)
        {
            return NetworkManager.NetworkInvoke<T>(this, this, methodName, args, maxTimeMs);
        }

        /// <summary>
        /// Preforms a non-blocking Network Invocation (Like an RPC). This will try to find the method on the current <see cref="NetworkClient"/>
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public void NetworkInvoke(string methodName, object[] args)
        {
            NetworkManager.NetworkInvoke(this, this, methodName, args);
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
                    Log.GlobalDebug($"Sending packet. Target: {packet.NetowrkIDTarget} Type: {packet.Type} CustomID: {packet.CustomPacketID} Length: {fullBytes.Length}");
                    Exception ex = Transport.Send(fullBytes, packet.Destination);
                    if (ex != null)
                    {
                        throw ex;
                    }
                    //Log.GlobalDebug("Packet sent!");
                }
                catch (Exception ex)
                {
                    Log.GlobalError("Failed to send packet! Error:\n" + ex.ToString());
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
                Log.GlobalError($"Invalid packet: {packet}");
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
                //Log.GlobalDebug("Encrypting using SYMMETRICAL");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.SymetricalEncrypted, true);
            }
            else if (currentEncryptionState >= (int)EncryptionState.AsymmetricalReady)
            {
                //Log.GlobalDebug("Encrypting using ASYMMETRICAL");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.AsymetricalEncrypted, true);
            }
            else
            {
                //Ensure the packet isnt ecnrypted if we don't support it.
                //Log.GlobalDebug("Encryption is not supported at this moment, ensuring it isn't flagged as being enabled on this packet.");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.AsymetricalEncrypted, false);
                packet.Flags = packet.Flags.SetFlag(PacketFlags.SymetricalEncrypted, false);
            }
            if (!packet.ValidateFlags())
            {
                Log.GlobalError($"Packet send failed! Flag validation failure. Packet Type: {packet.Type}, Target: {packet.NetowrkIDTarget}, Custom Packet ID: {packet.CustomPacketID}, Active Flags: {string.Join(", ", packet.Flags.GetActiveFlags())}");
                return null;
            }
            Log.GlobalDebug("Active Flags: " + string.Join(", ", packet.Flags.GetActiveFlags()));
            byte[] packetBytes = packet.Serialize().Data;
            byte[] packetHeaderBytes = packetBytes.Take(PacketHeader.HeaderLength - 4).ToArray();
            byte[] packetDataBytes = packetBytes.Skip(PacketHeader.HeaderLength - 4).ToArray();
            //StringBuilder hex = new StringBuilder(packetBytes.Length * 2);
            //foreach (byte b in packetBytes)
            //{
            //    hex.AppendFormat("{0:x2}", b);
            //}
            //Log.GlobalDebug("Raw Serialized Packet: \n" + hex.ToString());
            if (packet.Flags.HasFlag(PacketFlags.Compressed))
            {
                //Log.GlobalDebug("Compressing the packet.");
                packetDataBytes = packetDataBytes.Compress();
            }
            if (packet.Flags.HasFlag(PacketFlags.AsymetricalEncrypted))
            {
                if (currentEncryptionState < (int)EncryptionState.AsymmetricalReady)
                {
                    Log.GlobalError("Encryption cannot be done at this point: Not ready.");
                    return null;
                }
                if(packetDataBytes.Length > NetworkEncryptionManager.MaxBytesForAsym)
                {
                    Log.GlobalWarning($"Packet is too large for RSA! Packet Size: {packetDataBytes.Length}, Max Packet Size: {NetworkEncryptionManager.MaxBytesForAsym}");
                }
                else
                {
                    Log.GlobalDebug("Encrypting Packet: Asymmetrical");
                    packetDataBytes = EncryptionManager.Encrypt(packetDataBytes, false);
                }
            }
            if (packet.Flags.HasFlag(PacketFlags.SymetricalEncrypted))
            {
                if (currentEncryptionState < (int)EncryptionState.SymmetricalReady)
                {
                    Log.GlobalError("Encryption cannot be done at this point: Not ready.");
                    return null;
                }
                Log.GlobalDebug("Encrypting Packet: Symmetrical");
                packetDataBytes = EncryptionManager.Encrypt(packetDataBytes);
                if(packetDataBytes.Length == 0)
                {
                    Log.GlobalError("Encryption resulted in a null!");
                    return null;
                }
            }
            ByteWriter writer = new ByteWriter();
            byte[] packetFull = packetHeaderBytes.Concat(packetDataBytes).ToArray();
            Log.GlobalDebug($"Packet Size: Full (Raw): {packetBytes.Length}, Full (Processed): {packetFull.Length}. With Header Size: {packetFull.Length + 4}");
            writer.WriteInt(packetFull.Length);
            writer.Write(packetFull);
            int written = writer.DataLength;
            if(written != (packetFull.Length + 4))
            {
                Log.GlobalError($"Trying to send corrupted size! Custom Packet ID: {packet.CustomPacketID}, Target: {packet.NetowrkIDTarget}, Size: {written}, Expected: {packetFull.Length + 4}");
                return null;
            }
            byte[] fullBytes = writer.Data;
            if (fullBytes.Length > Packet.MaxPacketSize)
            {
                Log.GlobalError("Packet too large!");
                return null;
            }
            //StringBuilder hex1 = new StringBuilder(fullBytes.Length * 2);
            //foreach (byte b in fullBytes)
            //{
            //    hex1.AppendFormat("{0:x2}", b);
            //}
            //Log.GlobalDebug("Full Packet: \n" + hex1.ToString());
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
            Log.GlobalInfo($"Client thread started, ID {ClientID}");
            while (true)
            {
                if (_shuttingDown)
                {
                    Log.GlobalInfo("Shutting down loop");
                    break;
                }
                RawReader();
            }
            Log.GlobalInfo("Shutting down client, Closing socket.");
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
                Log.GlobalWarning("Transport recieved a null byte array.");
                return;
            }
            Deserialize(packet.Item1, packet.Item3);
            //if(!Transport.DataAvailable)
            //{
            //    //Log.GlobalDebug("No data available.");
            //    return;
            //}
            //byte[] buffer = new byte[Packet.MaxPacketSize]; // this can now be freely changed
            //Transport.BufferSize = Packet.MaxPacketSize;
            //int fillSize = 0; // the amount of bytes in the buffer. Reading anything from fillsize on from the buffer is undefined.
            //// this is for breaking a nested loop further down. thanks C#
            //if (!IsTransportConnected)
            //{
            //    Log.GlobalDebug("Disconnected!");
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
            //        Log.GlobalError(ex.ToString());
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
            //    Log.GlobalWarning("Got a malformed packet, Body Size can't be 0, Resetting header to beginning of Packet (may cuase duplicate packets)");
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
            //    Log.GlobalError("Body Size is corrupted! Raw: " + s);
            //}
            //while (fillSize < bodySize)
            //{
            //    //Log.Debug($"Trying to read bytes to read the body (we need at least {bodySize} and we have {fillSize})!");
            //    if (fillSize == buffer.Length)
            //    {
            //        // The buffer is too full, and we are fucked (oh shit)
            //        Log.GlobalError("Buffer became full before being able to read an entire packet. This probably means a packet was sent that was bigger then the buffer (Which is the packet max size). This is not recoverable, Disconnecting!");
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
            //        Log.GlobalError(ex.ToString());
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
            //Log.GlobalDebug(hex.ToString());
            PacketHeader header = Packet.ReadPacketHeader(fullPacket);
            if (header.Type == PacketType.CustomPacket && NetworkManager.GetCustomPacketByID(header.CustomPacketID) == null)
            {
                Log.GlobalWarning($"Got a packet with a Custom Packet ID that does not exist, either not registered or corrupt. Custom Packet ID: {header.CustomPacketID}, Target: {header.NetworkIDTarget}");
            }
            Log.GlobalDebug("Active Flags: " + string.Join(", ", header.Flags.GetActiveFlags()));
            Log.GlobalDebug($"Inbound Packet Info, Size Of Full Packet: {header.Size}, Type: {header.Type}, Target: {header.NetworkIDTarget}, CustomPacketID: {header.CustomPacketID}");
            byte[] rawPacket = fullPacket;
            byte[] headerBytes = fullPacket.Take(PacketHeader.HeaderLength).ToArray();
            byte[] packetBytes = fullPacket.Skip(PacketHeader.HeaderLength).ToArray();
            int currentEncryptionState = (int)EncryptionState;
            if (header.Flags.HasFlag(PacketFlags.SymetricalEncrypted))
            {
                Log.GlobalDebug("Trying to decrypt a packet using SYMMETRICAL encryption!");
                if (currentEncryptionState < (int)EncryptionState.SymmetricalReady)
                {
                    Log.GlobalError("Encryption cannot be done at this point: Not ready.");
                    return;
                }
                packetBytes = EncryptionManager.Decrypt(packetBytes);
            }
            if (header.Flags.HasFlag(PacketFlags.AsymetricalEncrypted))
            {
                Log.GlobalDebug("Trying to decrypt a packet using ASYMMETRICAL encryption!");
                if (currentEncryptionState < (int)EncryptionState.AsymmetricalReady)
                {
                    Log.GlobalError("Encryption cannot be done at this point: Not ready.");
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
                Log.GlobalWarning($"Header provided size is less then the actual packet length! Header: {header.Size}, Actual Packet Size: {fullPacket.Length - 4}");
            }
            fullPacket = headerBytes.Concat(packetBytes).ToArray();
            //StringBuilder hex1 = new StringBuilder(fullPacket.Length * 2);
            //foreach (byte b in fullPacket)
            //{
            //    hex1.AppendFormat("{0:x2}", b);
            //}
            //Log.GlobalDebug("Raw Deserialized Packet: \n" + hex1.ToString());
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
                        Log.GlobalError(ex.ToString());
                        SendError(ex.Message, LogSeverity.Error);
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
                        Log.GlobalError(ex.ToString());
                        SendError(ex.Message, LogSeverity.Error);
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
                        Log.GlobalError($"Disconnecting {ClientID} for " + connectionUpdatePacket.Reason);
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
                    if (NetworkServer.Config.DefaultReady)
                    {
                        Ready = true;
                    }
                    break;
                case PacketType.NetworkInvocation:
                    NetworkInvocationPacket networkInvocationPacket = new NetworkInvocationPacket();
                    networkInvocationPacket.Deserialize(data);
                    Log.GlobalDebug($"Network Invocation: ObjectID: {networkInvocationPacket.NetworkObjectTarget}, Method: {networkInvocationPacket.MethodName}, Arguments Count: {networkInvocationPacket.Arguments.Count}");
                    try
                    {
                        NetworkManager.NetworkInvoke(networkInvocationPacket, this);
                    }
                    catch (Exception ex)
                    {
                        Log.GlobalWarning($"Network Invocation Failed! Method {networkInvocationPacket.MethodName}, Error: {ex}");
                        NetworkInvocationResultPacket errorPacket = new NetworkInvocationResultPacket();
                        errorPacket.Success = false;
                        errorPacket.ErrorMessage = $"Method: {networkInvocationPacket.MethodName} Message: " + ex.Message;
                        errorPacket.Result = SerializedData.NullData;
                        errorPacket.CallbackID = networkInvocationPacket.CallbackID;
                        errorPacket.IgnoreResult = false;
                        Send(errorPacket);
                    }
                    break;
                case PacketType.NetworkInvocationResult:
                    NetworkInvocationResultPacket networkInvocationResultPacket = new NetworkInvocationResultPacket();
                    networkInvocationResultPacket.Deserialize(data);
                    Log.GlobalDebug($"NetworkInvocationResult: CallbackID: {networkInvocationResultPacket.CallbackID}, Success?: {networkInvocationResultPacket.Success}, Error Message: {networkInvocationResultPacket.ErrorMessage}");
                    NetworkManager.NetworkInvoke(networkInvocationResultPacket, this);
                    break;
                case PacketType.EncryptionPacket:
                    EncryptionPacket encryptionPacket = new EncryptionPacket();
                    encryptionPacket.Deserialize(data);
                    Log.GlobalInfo($"Encryption request! Function {encryptionPacket.EncryptionFunction}");
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
                            Log.GlobalInfo($"Got Asymmetrical Encryption Key, ID: {ClientID}");
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
                            Log.GlobalInfo($"Client Got Asymmetrical Encryption Key, ID: {ClientID}");
                            break;
                        case EncryptionFunction.SymetricalKeyRecieve:
                            EncryptionState = EncryptionState.SymmetricalReady;
                            Log.GlobalInfo($"Client Got Symmetrical Encryption Key, ID: {ClientID}");
                            EncryptionPacket updateEncryptionStateFinal = new EncryptionPacket();
                            updateEncryptionStateFinal.EncryptionFunction = EncryptionFunction.UpdateEncryptionStatus;
                            updateEncryptionStateFinal.State = EncryptionState.Encrypted;
                            Send(updateEncryptionStateFinal);
                            break;
                        default:
                            Log.GlobalError($"Invalid Encryption function: {encryptionPacket.EncryptionFunction}");
                            break;
                    }
                    break;
                default:
                    Log.GlobalError($"Packet is not handled! Info: Target: {header.NetworkIDTarget}, Type Provided: {header.Type}, Size: {header.Size}, Custom Packet ID: {header.CustomPacketID}");
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
                        Log.GlobalError(ex.ToString());
                        SendError(ex.Message, LogSeverity.Error);
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
                        Log.GlobalError(ex.ToString());
                        SendError(ex.Message, LogSeverity.Error);
                    }
                    break;
                case PacketType.ReadyStateUpdate:
                    ReadyStateUpdatePacket readyStateUpdatePacket = new ReadyStateUpdatePacket();
                    readyStateUpdatePacket.Deserialize(data);
                    _ready = readyStateUpdatePacket.Ready;
                    ReadyStateChanged?.Invoke(!_ready, _ready);
                    ClientReadyStateChanged?.Invoke(this);
                    NetworkManager.SendReadyPulse(this, Ready);
                    Log.GlobalInfo("New Client Ready State: " + _ready.ToString());
                    break;
                case PacketType.ServerData:
                    ServerDataPacket serverDataPacket = new ServerDataPacket();
                    serverDataPacket.Deserialize(data);
                    _clientId = serverDataPacket.YourClientID;
                    Log.GlobalInfo("New Client ID: " + _clientId.ToString());
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
                            Log.GlobalError($"Can't find packet with fullname {NewPacketPairs[i]}, this will cause more errors later!");
                            continue;
                        }
                        if (NetworkManager.AdditionalPacketTypes.ContainsKey(i))
                        {
                            if (!NetworkManager.IsDynamicAllocatedPacket(t))
                            {
                                Log.GlobalError("Tried to overwrite non-dynamic packet. Type: " + t.FullName);
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
                    Log.GlobalInfo("Finished re-writing dynamic packets: " + built);
                    break;
                case PacketType.ConnectionStateUpdate:
                    ConnectionUpdatePacket connectionUpdatePacket = new ConnectionUpdatePacket();
                    connectionUpdatePacket.Deserialize(data);
                    Log.GlobalInfo("New connection state: " + connectionUpdatePacket.State.ToString());
                    if (connectionUpdatePacket.State == ConnectionState.Disconnected)
                    {
                        //ruh roh
                        NetworkErrorData errorData = new NetworkErrorData("Disconnected by remote client. Reason: " + connectionUpdatePacket.Reason, false);
                        ConnectionError?.Invoke(errorData);
                        Log.GlobalError("Disconnected: " + connectionUpdatePacket.Reason);
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
                    NetworkInvocationPacket networkInvocationPacket = new NetworkInvocationPacket();
                    networkInvocationPacket.Deserialize(data);
                    Log.GlobalDebug($"Network Invocation: ObjectID: {networkInvocationPacket.NetworkObjectTarget}, Method: {networkInvocationPacket.MethodName}, Arguments Count: {networkInvocationPacket.Arguments.Count}");
                    try
                    {
                        NetworkManager.NetworkInvoke(networkInvocationPacket, this);
                    }
                    catch (Exception ex)
                    {
                        Log.GlobalWarning($"Network Invocation Failed! Method {networkInvocationPacket.MethodName}, Error: {ex}");
                        NetworkInvocationResultPacket errorPacket = new NetworkInvocationResultPacket();
                        errorPacket.Success = false;
                        errorPacket.ErrorMessage = $"Method: {networkInvocationPacket.MethodName} Message: " + ex.Message;
                        errorPacket.Result = SerializedData.NullData;
                        errorPacket.CallbackID = networkInvocationPacket.CallbackID;
                        errorPacket.IgnoreResult = false;
                        Send(errorPacket);
                    }
                    break;
                case PacketType.NetworkInvocationResult:
                    NetworkInvocationResultPacket networkInvocationResultPacket = new NetworkInvocationResultPacket();
                    networkInvocationResultPacket.Deserialize(data);
                    Log.GlobalDebug($"NetworkInvocationResult: CallbackID: {networkInvocationResultPacket.CallbackID}, Success?: {networkInvocationResultPacket.Success}, Error Message: {networkInvocationResultPacket.ErrorMessage}");
                    NetworkManager.NetworkInvoke(networkInvocationResultPacket, this);
                    break;
                case PacketType.EncryptionPacket:
                    EncryptionPacket encryptionPacket = new EncryptionPacket();
                    encryptionPacket.Deserialize(data);
                    EncryptionPacket encryptionRecieve = new EncryptionPacket();
                    Log.GlobalInfo($"Encryption request! Function {encryptionPacket.EncryptionFunction}");
                    switch (encryptionPacket.EncryptionFunction)
                    {
                        case EncryptionFunction.None:
                            break;
                        case EncryptionFunction.AsymmetricalKeySend:
                            EncryptionManager.OthersPublicKey = encryptionPacket.PublicKey;
                            EncryptionPacket gotYourPublicKey = new EncryptionPacket();
                            gotYourPublicKey.EncryptionFunction = EncryptionFunction.AsymmetricalKeyRecieve;
                            Log.GlobalInfo("Got Servers Public key, Sending mine.");
                            Send(gotYourPublicKey);
                            EncryptionState = EncryptionState.AsymmetricalReady;
                            encryptionRecieve.PublicKey = EncryptionManager.MyPublicKey;
                            encryptionRecieve.EncryptionFunction = EncryptionFunction.AsymmetricalKeySend;
                            Send(encryptionRecieve);
                            break;
                        case EncryptionFunction.SymmetricalKeySend:
                            Log.GlobalInfo($"Got servers symetrical key. Key: {string.Join("-", encryptionPacket.SymKey)}, IV: {string.Join("-", encryptionPacket.SymIV)}");
                            EncryptionManager.SharedAesKey = new Tuple<byte[], byte[]>(encryptionPacket.SymKey, encryptionPacket.SymIV);
                            EncryptionPacket gotYourSymmetricalKey = new EncryptionPacket();
                            gotYourSymmetricalKey.EncryptionFunction = EncryptionFunction.SymetricalKeyRecieve;
                            Send(gotYourSymmetricalKey);
                            EncryptionState = EncryptionState.SymmetricalReady;
                            break;
                        case EncryptionFunction.AsymmetricalKeyRecieve:
                            EncryptionState = EncryptionState.AsymmetricalReady;
                            Log.GlobalInfo("Server got my Asymmetrical key.");
                            break;
                        case EncryptionFunction.SymetricalKeyRecieve:
                            Log.GlobalError("Server should not be recieving my symmetrical key!");
                            break;
                        case EncryptionFunction.UpdateEncryptionStatus:
                            Log.GlobalInfo($"Server updated my encryption state: {encryptionPacket.State.ToString()}");
                            EncryptionState = encryptionPacket.State;
                            break;
                        default:
                            Log.GlobalError($"Invalid Encryption function: {encryptionPacket.EncryptionFunction}");
                            break;
                    }
                    break;
                default:
                    Log.GlobalError($"Packet is not handled! Info: Target: {header.NetworkIDTarget}, Type Provided: {header.Type}, Size: {header.Size}, Custom Packet ID: {header.CustomPacketID}");
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
            //Log.GlobalDebug("Reader ran");
        }

        /// <summary>
        /// Writes the next packet. (Blocking)
        /// </summary>
        internal void WriteNext()
        {
            RawWriter();
            //Log.GlobalDebug("Writer ran");
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
        public void SendError(string error, LogSeverity severity)
        {
            NetworkInvoke(nameof(GetError), new object[] { error, severity });
        }

        [NetworkInvocable(NetworkDirection.Any)]
        private void GetError(NetworkHandle handle, LogSeverity level, string err)
        {
            Log.Global(err, level);
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
