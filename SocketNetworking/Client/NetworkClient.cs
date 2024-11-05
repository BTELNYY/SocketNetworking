using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using System.Reflection;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Exceptions;
using System.Runtime.InteropServices;
using System.Net;
using System.Diagnostics;
using System.Security.Policy;
using System.Collections.Concurrent;
using System.Web;
using SocketNetworking.Misc;
using System.Runtime.CompilerServices;
using SocketNetworking.Transports;
using SocketNetworking.Shared;
using SocketNetworking.Server;

namespace SocketNetworking.Client
{
    public class NetworkClient
    {

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
        }

        public NetworkClient()
        {
            ClientCreated?.Invoke(this);
            _networkEncryptionManager = new NetworkEncryptionManager();
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

        private NetworkEncryptionManager _networkEncryptionManager;

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

        public bool IsConnected
        {
            get
            {
                return CurrentConnectionState == ConnectionState.Connected || CurrentConnectionState == ConnectionState.Handshake;
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
            if(NetworkServer.EncryptionMode == ServerEncryptionMode.Disabled)
            {
                return false;
            }
            if(NetworkServer.EncryptionMode == ServerEncryptionMode.Required)
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
            CallbackTimer<NetworkClient> timer = new CallbackTimer<NetworkClient>((x) => 
            {
                int encryptionState = (int)x.EncryptionState;
                if(encryptionState < 2)
                {
                    x.Disconnect("Failed Encryption Handshake.");
                }
            }, this, 10f);
            timer.Start();
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
            _packetReaderThread = new Thread(PacketReaderThreadMethod);
            _packetReaderThread.Start();
            _packetSenderThread = new Thread(PacketSenderThreadMethod);
            _packetSenderThread.Start();
        }

        /// <summary>
        /// Should be called locally to initialize the client, switching it from just being created to being ready to be used.
        /// </summary>
        public virtual void InitLocalClient()
        {
            _clientLocation = ClientLocation.Local;
            ClientConnected += OnLocalClientConnected;
        }


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
            _packetReaderThread?.Abort();
            _packetSenderThread?.Abort();
            _packetReaderThread = null;
            _packetSenderThread = null;
            if (Clients.Contains(this))
            {
                Clients.Remove(this);
            }
        }

        public T NetworkInvoke<T>(object target, string methodName, object[] args, float maxTimeMs = 5000)
        {
            return NetworkManager.NetworkInvoke<T>(target, this, methodName, args, maxTimeMs);
        }

        public void NetworkInvoke(object target, string methodName, object[] args)
        {
            NetworkManager.NetworkInvoke(target, this, methodName, args);
        }

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

        public T NetworkInvoke<T>(string methodName, object[] args, float maxTimeMs = 5000)
        {
            return NetworkManager.NetworkInvoke<T>(this, this, methodName, args, maxTimeMs);
        }
        public void NetworkInvoke(string methodName, object[] args)
        {
            NetworkManager.NetworkInvoke(this, this, methodName, args);
        }


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

        void StartClient()
        {
            if(CurrnetClientLocation == ClientLocation.Remote)
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


        protected static byte[] ShiftOut(ref byte[] input, int count)
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
                    if(clientDataPacket.Configuration.Protocol != NetworkServer.ServerConfiguration.Protocol)
                    {
                        Disconnect($"Server protocol mismatch. Expected: {NetworkServer.ServerConfiguration.Protocol} Got: {clientDataPacket.Configuration.Protocol}");
                        break;
                    }
                    if(clientDataPacket.Configuration.Version != NetworkServer.ServerConfiguration.Version)
                    {
                        Disconnect($"Server protocol mismatch. Expected: {NetworkServer.ServerConfiguration.Version} Got: {clientDataPacket.Configuration.Version}");
                        break;
                    }
                    if((clientDataPacket.PasswordHash != NetworkServer.ServerPassword.GetStringHash()) && NetworkServer.UseServerPassword)
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
                    if(NetworkServer.EncryptionMode == ServerEncryptionMode.Required)
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
        protected virtual void HandleLocalClient(PacketHeader header, byte[] data)
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
                    foreach(int i in NewPacketPairs.Keys)
                    {
                        Type t = Type.GetType(NewPacketPairs[i]);
                        if(t == null)
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
                    foreach(int i in NetworkManager.AdditionalPacketTypes.Keys)
                    {
                        built += $"ID: {i}, Fullname: {NetworkManager.AdditionalPacketTypes[i].FullName}\n";
                    }
                    Log.GlobalInfo("Finished re-writing dynamic packets: " + built);
                    break;
                case PacketType.ConnectionStateUpdate:
                    ConnectionUpdatePacket connectionUpdatePacket = new ConnectionUpdatePacket();
                    connectionUpdatePacket.Deserialize(data);
                    Log.GlobalInfo("New connection state: " + connectionUpdatePacket.State.ToString());
                    if(connectionUpdatePacket.State == ConnectionState.Disconnected)
                    {
                        //ruh roh
                        NetworkErrorData errorData = new NetworkErrorData("Disconnected by remote client. Reason: " + connectionUpdatePacket.Reason, false);
                        ConnectionError?.Invoke(errorData);
                        Log.GlobalError("Disconnected: " + connectionUpdatePacket.Reason);
                        StopClient();
                    }
                    if(connectionUpdatePacket.State == ConnectionState.Handshake)
                    {
                        _connectionState = ConnectionState.Handshake;
                    }
                    if(connectionUpdatePacket.State == ConnectionState.Connected)
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

        protected ConcurrentQueue<Packet> _toSendPackets = new ConcurrentQueue<Packet>();

        void PacketSenderThreadMethod()
        {
            while (true)
            {
                if (_manualPacketSend)
                {
                    continue;
                }
                SendNextPacketInternal();
            }
        }

        protected object streamLock = new object();

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
                    if(ex != null)
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
        }

        protected virtual void PreparePacket(ref Packet packet)
        {
            packet.Source = Transport.LocalEndPoint;
            bool validationSuccess = packet.ValidatePacket();
            if (!validationSuccess)
            {
                Log.GlobalError($"Invalid packet: {packet}");
            }
        }

        protected virtual byte[] SerializePacket(Packet packet)
        {
            Log.GlobalDebug("Active Flags: " + string.Join(", ", packet.Flags.GetActiveFlags()));
            int currentEncryptionState = (int)EncryptionState;
            if (currentEncryptionState > (int)EncryptionState.SymmetricalReady)
            {
                Log.GlobalDebug("Encrypting using SYMMETRICAL");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.SymetricalEncrypted, true);
            }
            else if (currentEncryptionState > (int)EncryptionState.AsymmetricalReady)
            {
                Log.GlobalDebug("Encrypting using ASYMMETRICAL");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.AsymtreicalEncrypted, true);
            }
            else
            {
                //Ensure the packet isnt ecnrypted if we don't support it.
                Log.GlobalDebug("Encryption is not supported at this moment, ensuring it isn't flagged as being enabled on this packet.");
                packet.Flags = packet.Flags.SetFlag(PacketFlags.AsymtreicalEncrypted, false);
                packet.Flags = packet.Flags.SetFlag(PacketFlags.SymetricalEncrypted, false);
            }
            if (!packet.ValidateFlags())
            {
                Log.GlobalError($"Packet send failed! Flag validation failure. Packet Type: {packet.Type}, Target: {packet.NetowrkIDTarget}, Custom Packet ID: {packet.CustomPacketID}, Active Flags: {string.Join(", ", packet.Flags.GetActiveFlags())}");
                return null;
            }
            byte[] packetBytes = packet.Serialize().Data;
            byte[] packetHeaderBytes = packetBytes.Take(PacketHeader.HeaderLength - 4).ToArray();
            byte[] packetDataBytes = packetBytes.Skip(PacketHeader.HeaderLength - 4).ToArray();
            if (packet.Flags.HasFlag(PacketFlags.Compressed))
            {
                Log.GlobalDebug("Compressing the packet.");
                packetDataBytes = packetDataBytes.Compress();
            }
            if (packet.Flags.HasFlag(PacketFlags.AsymtreicalEncrypted))
            {
                if (currentEncryptionState < (int)EncryptionState.AsymmetricalReady)
                {
                    Log.GlobalError("Encryption cannot be done at this point: Not ready.");
                    return null;
                }
                Log.GlobalDebug("Encrypting Packet: Asymmetrical");
                packetDataBytes = EncryptionManager.Encrypt(packetDataBytes, false);
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
            }
            ByteWriter writer = new ByteWriter();
            byte[] packetFull = packetHeaderBytes.Concat(packetDataBytes).ToArray();
            Log.GlobalDebug($"Packet Size: Full (Raw): {packetBytes.Length}, Full (Processed): {packetFull.Length}. With Header Size: {packetFull.Length + 4}");
            writer.WriteInt(packetFull.Length);
            writer.Write(packetFull);
            int written = packetFull.Length + 4;
            if (written > (packetFull.Length + 4))
            {
                Log.GlobalError($"Trying to send corrupted size! Custom Packet ID: {packet.CustomPacketID}, Target: {packet.NetowrkIDTarget}, Size: {written}, Expected: {packetBytes.Length + 4}");
                return null;
            }
            if(written < (packetFull.Length + 4))
            {
                Log.GlobalError($"Trying to send corrupted size! Custom Packet ID: {packet.CustomPacketID}, Target: {packet.NetowrkIDTarget}, Size: {written}, Expected: {packetBytes.Length + 4}");
            }
            byte[] fullBytes = writer.Data;
            if (fullBytes.Length > Packet.MaxPacketSize)
            {
                Log.GlobalError("Packet too large!");
                return null;
            }
            return fullBytes;
        }

        protected virtual void Deserialize(byte[] fullPacket, IPEndPoint endpoint)
        {
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
                if (currentEncryptionState == (int)EncryptionState.SymmetricalReady)
                {
                    Log.GlobalError("Encryption cannot be done at this point: Not ready.");
                    return;
                }
                packetBytes = EncryptionManager.Decrypt(packetBytes);
            }
            if (header.Flags.HasFlag(PacketFlags.AsymtreicalEncrypted))
            {
                Log.GlobalDebug("Trying to decrypt a packet using ASYMMETRICAL encryption!");
                if (currentEncryptionState == (int)EncryptionState.AsymmetricalReady)
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

        protected virtual void PacketReaderThreadMethod()
        {
            Log.GlobalInfo($"Client thread started, ID {ClientID}");
            //int waitingSize = 0;
            //byte[] prevPacketFragment = { };
            byte[] buffer = new byte[Packet.MaxPacketSize]; // this can now be freely changed
            Transport.BufferSize = Packet.MaxPacketSize;
            int fillSize = 0; // the amount of bytes in the buffer. Reading anything from fillsize on from the buffer is undefined.
            while (true)
            {
            Packet: // this is for breaking a nested loop further down. thanks C#
                if (_shuttingDown)
                {
                    Log.GlobalInfo("Shutting down loop");
                    break;
                }
                if (!IsTransportConnected)
                {
                    Log.GlobalDebug("Disconnected!");
                    StopClient();
                    return;
                }
                /*if(TcpClient.ReceiveBufferSize == 0)
                {
                    continue;
                }*/
                /*if (!NetworkStream.DataAvailable)
                {
                    //Log.Debug("Nothing to read on stream");
                    continue;
                }*/
                //Log.Debug(TcpClient.ReceiveBufferSize.ToString());
                if (fillSize < sizeof(int))
                {
                    // we dont have enough data to read the length data
                    //Log.Debug($"Trying to read bytes to get length (we need at least 4 we have {fillSize})!");
                    int count = 0;
                    try
                    {
                        int tempFillSize = fillSize;
                        //(byte[], Exception) transportRead = Transport.Receive(fillSize, buffer.Length - fillSize);
                        (byte[], Exception, IPEndPoint) transportRead = Transport.Receive(0, buffer.Length - fillSize);
                        count = transportRead.Item1.Length;
                        buffer = Transport.Buffer;
                        //count = NetworkStream.Read(tempBuffer, 0, buffer.Length - fillSize);
                    }
                    catch(Exception ex)
                    {
                        Log.GlobalError(ex.ToString());
                        continue;
                    }
                    fillSize += count;
                    //Log.Debug($"Read {count} bytes from buffer ({fillSize})!");
                    continue;
                }
                int bodySize = BitConverter.ToInt32(buffer, 0); // i sure do hope this doesnt modify the buffer.
                bodySize = IPAddress.NetworkToHostOrder(bodySize);
                if (bodySize == 0)
                {
                    Log.GlobalWarning("Got a malformed packet, Body Size can't be 0, Resetting header to beginning of Packet (may cuase duplicate packets)");
                    fillSize = 0;
                    continue;
                }
                fillSize -= sizeof(int); // this kinda desyncs fillsize from the actual size of the buffer, but eh
                // read the rest of the whole packet
                if(bodySize > Packet.MaxPacketSize || bodySize < 0)
                {
                    CurrentConnectionState = ConnectionState.Disconnected;
                    string s = string.Empty;
                    for(int i = 0; i < buffer.Length; i++)
                    {
                        s += Convert.ToString(buffer[i], 2).PadLeft(8, '0') + " ";
                    }
                    Log.GlobalError("Body Size is corrupted! Raw: " + s);
                }
                while (fillSize < bodySize)
                {
                    //Log.Debug($"Trying to read bytes to read the body (we need at least {bodySize} and we have {fillSize})!");
                    if (fillSize == buffer.Length)
                    {
                        // The buffer is too full, and we are fucked (oh shit)
                        Log.GlobalError("Buffer became full before being able to read an entire packet. This probably means a packet was sent that was bigger then the buffer (Which is the packet max size). This is not recoverable, Disconnecting!");
                        Disconnect("Illegal Packet Size");
                        break;
                    }
                    int count;
                    try
                    {
                        (byte[], Exception, IPEndPoint) transportRead = Transport.Receive(fillSize, buffer.Length - fillSize);
                        count = transportRead.Item1.Length;
                        buffer = Transport.Buffer;
                        //count = NetworkStream.Read(buffer, fillSize, buffer.Length - fillSize);
                    }
                    catch(Exception ex)
                    {
                        Log.GlobalError(ex.ToString());
                        goto Packet;
                    }
                    fillSize += count;
                }
                // we now know we have enough bytes to read at least one whole packet;
                byte[] fullPacket = ShiftOut(ref buffer, bodySize + sizeof(int));
                if((fillSize -= bodySize) < 0)
                {
                    fillSize = 0;
                }
                //fillSize -= bodySize; // this resyncs fillsize with the fullness of the buffer
                //Log.Debug($"Read full packet with size: {fullPacket.Length}");

                Deserialize(fullPacket, Transport.Peer);
            }
            Log.GlobalInfo("Shutting down client, Closing socket.");
            Transport.Close();
        }

        protected ConcurrentQueue<ReadPacketInfo> _toReadPackets = new ConcurrentQueue<ReadPacketInfo>();

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

        protected struct ReadPacketInfo
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
