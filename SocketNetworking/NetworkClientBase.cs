using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.Misc;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;

namespace SocketNetworking
{
    public class NetworkClientBase : INetworkClient
    {
        /// <summary>
        /// Forcefully remove client on destruction
        /// </summary>
        ~NetworkClientBase()
        {
            ClientDestroyed?.Invoke(ClientID);
            if (Clients.Contains(this))
            {
                Clients.Remove(this);
            }
        }

        public NetworkClientBase()
        {
            ClientCreated?.Invoke(this);
            networkEncryptionManager = new NetworkEncryptionManager();
            Init();
        }

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

        /// <summary>
        /// Called when the state of the <see cref="NetworkClientBase.Ready"/> variable changes. First variable is the old state, and the second is the new state. This event is fired on both Local and Remote clients.
        /// </summary>
        public event Action<bool, bool> ReadyStateChanged;

        /// <summary>
        /// Called on Local and Remote clients when a full packet is read.
        /// </summary>
        public event Action<PacketHeader, byte[]> PacketRead;

        /// <summary>
        /// Called when a packet is ready to handle, this event is never called if <see cref="NetworkClientBase.ManualPacketHandle"/> is set to false
        /// </summary>
        public event Action<PacketHeader, byte[]> PacketReadyToHandle;

        /// <summary>
        /// Called when a packet is ready to send, this event is never called if <see cref="NetworkClientBase.ManualPacketSend"/> is set to false.
        /// </summary>
        public event Action<Packet> PacketReadyToSend;
        #endregion

        #region Static Events

        /// <summary>
        /// Called when any clients <see cref="Ready"/> state changes
        /// </summary>
        public static event Action<NetworkClientBase> ClientReadyStateChanged;

        /// <summary>
        /// Called when any clients <see cref="CurrentConnectionState"/> is changed
        /// </summary>
        public static event Action<NetworkClientBase> ClientConnectionStateChanged;

        /// <summary>
        /// Called when a network client is destroyed, gives the clients ID.
        /// </summary>
        public static event Action<int> ClientDestroyed;

        /// <summary>
        /// Called when a client is created, gives the <see cref="NetworkClientBase"/> that was created.
        /// </summary>
        public static event Action<NetworkClientBase> ClientCreated;

        #endregion

        /// <summary>
        /// Only has instances on the local client. Use <see cref="NetworkServer.ConnectedClients"/> for server side clients.
        /// </summary>
        public readonly static HashSet<NetworkClientBase> Clients = new HashSet<NetworkClientBase>();


        #region Properties

        /// <summary>
        /// Returns the amount of <see cref="Packet"/>s left to read, this is always zero if <see cref="NetworkClientBase.ManualPacketHandle"/> is false
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

        /// <summary>
        /// Returns the Address to which the socket is connected too, In the format IP:Port
        /// </summary>
        public virtual string ConnectedIPAndPort
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns the connection IP
        /// </summary>
        public virtual string ConnectedIP
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns the connection port
        /// </summary>
        public virtual int ConnectedPort
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected bool _ready = false;

        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                if (!IsConnected || CurrentConnectionState != ConnectionState.Connected)
                {
                    Log.GlobalWarning("Can't change ready state becuase the socket is not connected or the handshake isn't done.");
                    return;
                }
                if (CurrnetClientLocation == ClientLocation.Remote)
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
        /// If true, the library will not handle packets automatically instead queueing them, you must call <see cref="NetworkClientBase.HandleNextPacket"/> to handle the next packet.
        /// </summary>
        public bool ManualPacketHandle
        {
            get => _manualPacketHandle;
            set
            {
                if (CurrentConnectionState == ConnectionState.Handshake)
                {
                    Log.GlobalWarning("Changing Packet read mode while the handshake has not yet finished, this may cause issues!");
                }
                _manualPacketHandle = value;
            }
        }

        /// <summary>
        /// Prevents the library from automatically sending packets, instead waiting for a call to <see cref="NetworkClientBase.SendNextPacket()"/>
        /// </summary>
        public bool ManualPacketSend
        {
            get => _manualPacketSend;
            set
            {
                if (CurrentConnectionState == ConnectionState.Handshake)
                {
                    Log.GlobalWarning("Changing Packet write mode while in handshake, things may break!");
                }
                _manualPacketSend = value;
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
                if (CurrnetClientLocation != ClientLocation.Remote)
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

        public EncryptionState EncryptionState
        {
            get
            {
                return _encryptionState;
            }
            protected set
            {
                _encryptionState = value;
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
                if (IsConnected)
                {
                    Log.GlobalError("Can't update NetworkConfiguration while client is connected.");
                    return;
                }
                _clientConfiguration = value;
            }
        }


        private bool _shuttingDown = false;


        public NetworkEncryptionManager EncryptionManager => networkEncryptionManager;

        private NetworkEncryptionManager networkEncryptionManager;

        public virtual bool IsConnected => throw new NotImplementedException();

        #endregion

        #region Encryption Requests

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


        [NetworkInvocable(PacketDirection.Client)]
        private bool ServerGetEncryptionRequest()
        {
            if (NetworkServer.EncryptionMode == ServerEncryptionMode.Disabled)
            {
                return false;
            }
            if (NetworkServer.EncryptionMode == ServerEncryptionMode.Required)
            {
                return true;
            }
            ServerBeginEncryption();
            return true;
        }

        public void ServerBeginEncryption()
        {
            EncryptionPacket packet = new EncryptionPacket();
            packet.EncryptionFunction = EncryptionFunction.AsymmetricalKeySend;
            packet.PublicKey = EncryptionManager.MyPublicKey;
            _encryptionState = EncryptionState.Handshake;
            Send(packet);
            CallbackTimer<NetworkClientBase> timer = new CallbackTimer<NetworkClientBase>((x) =>
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


        #region Start And Stop

        /// <summary>
        /// Should be called locally to initialize the client, switching it from just being created to being ready to be used.
        /// </summary>
        public virtual void InitLocalClient()
        {
            _clientLocation = ClientLocation.Local;
            ClientConnected += OnLocalClientConnected;
        }

        public virtual void InitRemoteClient(int clientId, object client)
        {
            _clientId = clientId;
            _clientLocation = ClientLocation.Remote;
            ClientConnected += OnRemoteClientConnected;
            ClientConnected?.Invoke();
            _packetReaderThread = new Thread(PacketReaderThreadMethod);
            _packetReaderThread.Start();
            _packetSenderThread = new Thread(PacketSenderThreadMethod);
            _packetSenderThread.Start();
        }

        protected virtual void OnLocalClientConnected()
        {
            if (CurrnetClientLocation != ClientLocation.Local)
            {
                return;
            }
            ClientDataPacket dataPacket = new ClientDataPacket(_clientPassword);
            Send(dataPacket);
        }

        public virtual void StopClient()
        {
            NetworkManager.SendDisconnectedPulse(this);
            _connectionState = ConnectionState.Disconnected;
            if (CurrnetClientLocation == ClientLocation.Remote)
            {
                NetworkServer.RemoveClient(ClientID);
            }
            _shuttingDown = true;
            _packetReaderThread.Abort();
            _packetSenderThread.Abort();
            _packetReaderThread = null;
            _packetSenderThread = null;
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

        #region Packet Handlers
        /// <summary>
        /// Only called on the server.
        /// </summary>
        private void HandleRemoteClient(PacketHeader header, byte[] data)
        {
            switch (header.Type)
            {
                case PacketType.CustomPacket:
                    NetworkManager.TriggerPacketListeners(header, data, this);
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
                    if ((clientDataPacket.PasswordHash != NetworkServer.ServerPassword.GetStringHash()) && NetworkServer.UseServerPassword)
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
                    if (NetworkServer.EncryptionMode == ServerEncryptionMode.Required)
                    {
                        ServerBeginEncryption();
                    }
                    if (NetworkServer.DefaultReady)
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
                            EncryptionState = EncryptionState.AsymmetricalReady;
                            Log.GlobalInfo($"Client Got Asymmetrical Encryption Key, ID: {ClientID}");
                            break;
                        case EncryptionFunction.SymetricalKeyRecieve:
                            EncryptionState = EncryptionState.SymmetricalReady;
                            Log.GlobalInfo($"Client Got Symmetrical Encryption Key, ID: {ClientID}");
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
        private void HandleLocalClient(PacketHeader header, byte[] data)
        {
            switch (header.Type)
            {
                case PacketType.CustomPacket:
                    NetworkManager.TriggerPacketListeners(header, data, this);
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
                            Send(gotYourPublicKey);
                            encryptionRecieve.PublicKey = EncryptionManager.MyPublicKey;
                            encryptionRecieve.EncryptionFunction = EncryptionFunction.AsymmetricalKeySend;
                            Send(encryptionRecieve);
                            break;
                        case EncryptionFunction.SymmetricalKeySend:
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

        private ConcurrentQueue<Packet> _toSendPackets = new ConcurrentQueue<Packet>();

        public virtual void PacketSenderThreadMethod()
        {
            while (true)
            {
                if (_manualPacketSend)
                {
                    continue;
                }
                if (_toSendPackets.IsEmpty)
                {
                    continue;
                }
                _toSendPackets.TryDequeue(out Packet packet);
                SendNextPacketInternal(packet);
            }
        }

        protected virtual void SendNextPacketInternal(Packet packet)
        {
            SendImmediate(packet);
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

        #region Network RPC

        public virtual T NetworkInvoke<T>(object target, string methodName, object[] args, float maxTimeMs = 5000)
        {
            return NetworkManager.NetworkInvoke<T>(target, this, methodName, args, maxTimeMs);
        }

        public virtual void NetworkInvoke(object target, string methodName, object[] args)
        {
            NetworkManager.NetworkInvoke(target, this, methodName, args);
        }

        public virtual T NetworkInvoke<T>(string methodName, object[] args, float maxTimeMs = 5000)
        {
            return NetworkManager.NetworkInvoke<T>(this, this, methodName, args, maxTimeMs);
        }
        public virtual void NetworkInvoke(string methodName, object[] args)
        {
            NetworkManager.NetworkInvoke(this, this, methodName, args);
        }

        #endregion

        #region Connection Managment

        /// <summary>
        /// Connect to a remote host. Only used on the Local Client.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual bool Connect(string host, int port, string password)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Disconnects the connection with the reason "Disconnected"
        /// </summary>
        public virtual void Disconnect()
        {
            Disconnect("Disconnected");
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
            Send(connectionUpdatePacket);
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

        #endregion

        #region Raw Data Handling

        public virtual byte[] Recieve()
        {
            throw new NotImplementedException();
        }

        public virtual bool Send(byte[] data)
        {
            throw new NotImplementedException();
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

        public virtual void Send(Packet packet)
        {
            if (!IsConnected)
            {
                Log.GlobalWarning("Can't Send packet, not connected!");
                ConnectionError?.Invoke(new NetworkErrorData("Tried to send packets while not connected.", IsConnected));
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

        public virtual void SendImmediate(Packet packet)
        {
            if (!packet.ValidateFlags())
            {
                Log.GlobalError($"Packet send failed! Flag validation failure. Packet Type: {packet.Type}, Target: {packet.NetowrkIDTarget}, Custom Packet ID: {packet.CustomPacketID}, Active Flags: {string.Join(", ", packet.Flags.GetActiveFlags())}");
                return;
            }
            byte[] packetBytes = packet.Serialize().Data;
            byte[] packetHeaderBytes = packetBytes.Take(PacketHeader.HeaderLength - 4).ToArray();
            byte[] packetDataBytes = packetBytes.Skip(PacketHeader.HeaderLength - 4).ToArray();
            //Log.GlobalDebug("Active Flags: " + string.Join(", ", packet.Flags.GetActiveFlags()));
            if (packet.Flags.HasFlag(PacketFlags.Compressed))
            {
                //Log.GlobalDebug("Compressing the packet.");
                packetDataBytes = packetDataBytes.Compress();
            }
            int currentEncryptionState = (int)EncryptionState;
            if (currentEncryptionState >= (int)EncryptionState.SymmetricalReady)
            {
                //Log.GlobalDebug("Encrypting using SYMMETRICAL");
                packet.Flags.SetFlag(PacketFlags.SymetricalEncrypted, true);
            }
            else if (currentEncryptionState >= (int)EncryptionState.AsymmetricalReady)
            {
                //Log.GlobalDebug("Encrypting using ASYMMETRICAL");
                packet.Flags.SetFlag(PacketFlags.AsymtreicalEncrypted, true);
            }
            if (packet.Flags.HasFlag(PacketFlags.AsymtreicalEncrypted))
            {
                if (currentEncryptionState < (int)EncryptionState.AsymmetricalReady)
                {
                    Log.GlobalError("Encryption cannot be done at this point: Not ready.");
                    return;
                }
                //Log.GlobalDebug("Encrypting Packet: Asymmetrical");
                packetDataBytes = EncryptionManager.Encrypt(packetDataBytes, false);
            }
            if (packet.Flags.HasFlag(PacketFlags.SymetricalEncrypted))
            {
                if (currentEncryptionState < (int)EncryptionState.SymmetricalReady)
                {
                    Log.GlobalError("Encryption cannot be done at this point: Not ready.");
                    return;
                }
                //Log.GlobalDebug("Encrypting Packet: Symmetrical");
                packetDataBytes = EncryptionManager.Encrypt(packetDataBytes);
            }
            ByteWriter writer = new ByteWriter();
            byte[] packetFull = packetHeaderBytes.Concat(packetDataBytes).ToArray();
            Log.GlobalDebug($"Packet Size: Full (Raw): {packetBytes.Length}, Full (Processed): {packetFull.Length}. With Header Size: {packetFull.Length + 4}");
            writer.WriteInt(packetFull.Length);
            writer.Write(packetFull);
            int written = packetFull.Length;
            if (written > (packetFull.Length + 4))
            {
                Log.GlobalError($"Trying to send corrupted size! Custom Packet ID: {packet.CustomPacketID}, Target: {packet.NetowrkIDTarget}, Size: {written}, Expected: {packetBytes.Length + 4}");
                return;
            }
            byte[] fullBytes = writer.Data;
            if (fullBytes.Length > Packet.MaxPacketSize)
            {
                Log.GlobalError("Packet too large!");
                return;
            }
            Send(fullBytes);
        }

        #endregion

        //Util.

        private static byte[] ShiftOut(ref byte[] input, int count)
        {
            byte[] output = new byte[count];
            // copy the first N elements to output
            Buffer.BlockCopy(input, 0, output, 0, count);
            // copy the back of the array forward (removing the elements we copied to output)
            // ie with count 2: [1, 2, 3, 4] -> [3, 4, 3, 4]
            // we dont update the end of the array to fill with zeros, because we dont need to (we just call it undefined behavior)
            Buffer.BlockCopy(input, count, input, 0, count);
            return output;
        }
    }

    public struct ReadPacketInfo
    {
        public PacketHeader Header;
        public byte[] Data;
    }

    /// <summary>
    /// Represents the encryption state withe the remote client/server.
    /// </summary>
    public enum EncryptionState : byte
    {
        Disabled,
        Handshake,
        AsymmetricalReady,
        SymmetricalReady,
        Encrypted
    }

    /// <summary>
    /// The current state of the handshake. Disconnect = Either not connected at all or just got disconnected. Handshake = Client-Server still agreeing on protocol and version. Connected = System connected.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Handshake,
        Connected,
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
