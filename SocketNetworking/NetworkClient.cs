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

namespace SocketNetworking
{
    public class NetworkClient
    {
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
        /// Called when the state of the <see cref="NetworkClient.Ready"/> variable changes. First variable is the old state, and the second is the new state. This event is fired on both Local and Remote clients.
        /// </summary>
        public event Action<bool, bool> ReadyStateChanged;

        /// <summary>
        /// Called on Local and Remote clients when a full packet is read.
        /// </summary>
        public event Action<PacketHeader, byte[]> PacketRead;
        #endregion

        #region Static Events

        public static event Action<NetworkClient> ClientReadyStateChanged;

        public static event Action<NetworkClient> ClientConnectionStateChanged;

        #endregion

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

        private TcpClient _tcpClient;

        /// <summary>
        /// The current TcpClient reference
        /// </summary>
        public TcpClient TcpClient
        {
            get
            {
                return _tcpClient;
            }
        }

        /// <summary>
        /// Returns the Address to which the socket is connected too, In the format IP:Port
        /// </summary>
        public string ConnectedIPAndPort
        {
            get
            {
                IPEndPoint remoteIpEndPoint = TcpClient.Client.RemoteEndPoint as IPEndPoint;
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
                IPEndPoint remoteIpEndPoint = TcpClient.Client.RemoteEndPoint as IPEndPoint;
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
                IPEndPoint remoteIpEndPoint = TcpClient.Client.RemoteEndPoint as IPEndPoint;
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
                if(!IsConnected || CurrentConnectionState != ConnectionState.Connected) 
                {
                    Log.Warning("Can't change ready state becuase the socket is not connected or the handshake isn't done.");
                    return;
                }
                if(CurrnetClientLocation == ClientLocation.Remote) 
                {
                    _ready = value;
                    ReadyStateChanged?.Invoke(!_ready, _ready);
                    ClientReadyStateChanged.Invoke(this);
                }
                ReadyStateUpdatePacket readyStateUpdatePacket = new ReadyStateUpdatePacket
                {
                    Ready = value
                };
                Send(readyStateUpdatePacket);
            }
        }

        /// <summary>
        /// The <see cref="System.Net.Sockets.TcpClient"/>s <see cref="System.Net.Sockets.NetworkStream"/>
        /// </summary>
        public NetworkStream NetworkStream
        {
            get
            {
                return TcpClient.GetStream();
            }
        }

        /// <summary>
        /// <see cref="bool"/> which determines if the client has connected to a server
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if(CurrnetClientLocation == ClientLocation.Remote && TcpClient.Connected)
                {
                    return true;
                }
                if(TcpClient != null) 
                {
                    return TcpClient.Connected;
                }
                return false;
            }
        }

        /// <summary>
        /// <see cref="bool"/> which represents if the Client has been started
        /// </summary>
        public bool ClientStarted
        {
            get
            {
                if(CurrnetClientLocation == ClientLocation.Remote)
                {
                    return true;
                }
                return _clientActive;
            }
        }

        private ConnectionState _connectionState = ConnectionState.Disconnected;

        /// <summary>
        /// The <see cref="ConnectionState"/> of the current client. Can only be set by clients which have the <see cref="ClientLocation.Remote"/> <see cref="CurrnetClientLocation"/>
        /// </summary>
        public ConnectionState CurrentConnectionState
        {
            get
            {
                return _connectionState;
            }
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
                ClientConnectionStateChanged.Invoke(this);
            }
        }

        private bool _clientActive = false;

        private string _clientPassword = "DefaultPassword";

        public string PasswordHash
        {
            get
            {
                return _clientPassword.GetStringHash();
            }
        }

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
                    Log.Error("Can't update NetworkConfiguration while client is connected.");
                    return;
                }
                _clientConfiguration = value;
            }
        }


        private bool _shuttingDown = false;

        /// <summary>
        /// Used when initializing a <see cref="NetworkClient"/> object on the server. Do not call this on the local client.
        /// </summary>
        /// <param name="clientId">
        /// Given ClientID
        /// </param>
        /// <param name="socket">
        /// The <see cref="System.Net.Sockets.TcpClient"/> object which handles data transport.
        /// </param>
        public void InitRemoteClient(int clientId, TcpClient socket)
        {
            _clientId = clientId;
            _tcpClient = socket;
            //_tcpClient.NoDelay = true;
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
        public void InitLocalClient()
        {
            _clientLocation = ClientLocation.Local;
            ClientConnected += OnLocalClientConnected;
        }

        void OnLocalClientConnected()
        {
            if(CurrnetClientLocation != ClientLocation.Local)
            {
                return;
            }
            ClientDataPacket dataPacket = new ClientDataPacket(_clientPassword);
            Send(dataPacket);
        }

        void OnRemoteClientConnected()
        {
            if(CurrnetClientLocation != ClientLocation.Remote)
            {
                return;
            }
            CurrentConnectionState = ConnectionState.Handshake;
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
                Log.Error("Cannot connect to other servers from remote.");
                return false;
            }
            if (IsConnected)
            {
                Log.Error("Can't connect: Already connected to a server.");
                return false;
            }
            _tcpClient = new TcpClient();
            //_tcpClient.NoDelay = true;
            try
            {
                _tcpClient.Connect(hostname, port);
            }
            catch(Exception ex)
            {
                Log.Error($"Failed to connect: \n {ex}");
                return false;
            }
            _clientPassword = password;
            StartClient();
            return true;
        }

        void StartClient()
        {
            if(CurrnetClientLocation == ClientLocation.Remote)
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
            ClientConnected?.Invoke();
        }


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
                    }
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
                        Configuration = NetworkServer.ServerConfiguration
                    };
                    Send(serverDataPacket);
                    CurrentConnectionState = ConnectionState.Connected;
                    if (NetworkServer.DefaultReady)
                    {
                        Ready = true;
                    }
                    break;
                default:
                    Log.Error("Packet is not handled!");
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
                    ClientReadyStateChanged.Invoke(this);
                    Log.Info("New Client Ready State: " + _ready.ToString());
                    break;
                case PacketType.ServerData:
                    ServerDataPacket serverDataPacket = new ServerDataPacket();
                    serverDataPacket.Deserialize(data);
                    _clientId = serverDataPacket.YourClientID;
                    Log.Info("New Client ID: " + _clientId.ToString());
                    if (serverDataPacket.Configuration.Protocol != ClientConfiguration.Protocol || serverDataPacket.Configuration.Version != ClientConfiguration.Version)
                    {
                        Disconnect($"Server protocol mismatch. Expected: {ClientConfiguration} Got: {serverDataPacket.Configuration}");
                        break;
                    }
                    break;
                case PacketType.ConnectionStateUpdate:
                    ConnectionUpdatePacket connectionUpdatePacket = new ConnectionUpdatePacket();
                    connectionUpdatePacket.Deserialize(data);
                    Log.Info("New connection state: " + connectionUpdatePacket.State.ToString());
                    if(connectionUpdatePacket.State == ConnectionState.Disconnected)
                    {
                        //ruh roh
                        Log.Error("Disconnected: " + connectionUpdatePacket.Reason);
                        StopClient();
                    }
                    if(connectionUpdatePacket.State == ConnectionState.Handshake)
                    {
                        _connectionState = ConnectionState.Handshake;
                    }
                    if(connectionUpdatePacket.State == ConnectionState.Connected)
                    {
                        _connectionState = ConnectionState.Connected;
                    }
                    ClientConnectionStateChanged.Invoke(this);
                    break;
                default:
                    Log.Error("Packet is not handled!");
                    break;
            }
        }

        private readonly ConcurrentQueue<Packet> _toSendPackets = new ConcurrentQueue<Packet>();

        void PacketSenderThreadMethod()
        {
            while (true)
            {
                if(_toSendPackets.IsEmpty)
                {
                    continue;
                }
                foreach(Packet packet in _toSendPackets)
                {
                    Log.Info("Sending packet. Type: " + packet.Type.ToString());
                    NetworkStream serverStream = NetworkStream;
                    byte[] packetBytes = packet.Serialize().Data;
                    ByteWriter writer = new ByteWriter();
                    writer.WriteInt(packetBytes.Length);
                    writer.Write(packetBytes);
                    byte[] fullBytes = writer.Data;
                    if (packetBytes.Length > Packet.MaxPacketSize)
                    {
                        Log.Error("Packet too large!");
                        return;
                    }
                    serverStream.Write(fullBytes, 0, fullBytes.Length);
                }
            }
        }

        void PacketReaderThreadMethod()
        {
            Log.Info($"Client thread started, ID {ClientID}");
            _tcpClient.NoDelay = false;
            //int waitingSize = 0;
            //byte[] prevPacketFragment = { };
            byte[] buffer = new byte[Packet.MaxPacketSize]; // this can now be freely changed
            int fillSize = 0; // the amount of bytes in the buffer. Reading anything from fillsize on from the buffer is undefined. 
            while (true)
            {
            Packet: // this is for breaking a nested loop further down. thanks C#
                if (_shuttingDown)
                {
                    Log.Info("Shutting down loop");
                    break;
                }
                if (!IsConnected)
                {
                    Log.Debug("Disconnected!");
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
                    Log.Debug($"Trying to read bytes to get length (we need at least 4 we have {fillSize})!");
                    int count;
                    try
                    {
                        count = NetworkStream.Read(buffer, fillSize, buffer.Length - fillSize);
                    }
                    catch
                    {
                        continue;
                    }
                    fillSize += count;
                    Log.Debug($"Read {count} bytes from buffer ({fillSize})!");
                    continue;
                }

                int bodySize = BitConverter.ToInt32(buffer, 0); // i sure do hope this doesnt modify the buffer.
                fillSize -= sizeof(int); // this kinda desyncs fillsize from the actual size of the buffer, but eh
                // read the rest of the whole packet
                while (fillSize < bodySize)
                {
                    Log.Debug($"Trying to read bytes to read the body (we need at least {bodySize} and we have {fillSize})!");
                    if (fillSize == buffer.Length)
                    {
                        // The buffer is too full, and we are fucked (oh shit)
                        Log.Error("Buffer became full before being able to read an entire packet. This probably means a packet was sent that was bigger then the buffer (Which is the packet max size)");
                        throw new Exception("We are fucked!");
                    }
                    int count;
                    try
                    {
                        count = NetworkStream.Read(buffer, fillSize, buffer.Length - fillSize);
                    }
                    catch
                    {
                        goto Packet;
                    }
                    fillSize += count;
                    Log.Debug($"Read {count} bytes from buffer ({fillSize})!");
                }


                // we now know we have enough bytes to read at least one whole packet;
                byte[] fullPacket = ShiftOut(ref buffer, bodySize + sizeof(int));
                fillSize -= bodySize; // this resyncs fillsize with the fullness of the buffer
                Log.Debug($"Read full packet with size: {fullPacket.Length}");
                PacketHeader header = Packet.ReadPacketHeader(fullPacket);
                Log.Debug($"Inbound Packet Info, Size Of Full Packet: {header.Size}, Type: {header.Type}, Target: {header.NetworkIDTarget}, CustomPacketID: {header.CustomPacketID}");
                PacketRead?.Invoke(header, fullPacket);
                if (CurrnetClientLocation == ClientLocation.Remote)
                {
                    HandleRemoteClient(header, fullPacket);
                }
                if (CurrnetClientLocation == ClientLocation.Local)
                {
                    HandleLocalClient(header, fullPacket);
                }


                // any leftover data in the buffer is recycled for the next iteration 
            }
            Log.Info("Shutting down client, Closing socket.");
            _tcpClient.Close();
        }
        /// <summary>
        /// Sends any <see cref="Packet"/> down the network stream to whatever is connected on the other side. Note that this method doesn't check who it is sending it to, instead sending it to the current stream.
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> to send down the stream.
        /// </param>
        public void Send(Packet packet)
        {
            if (!IsConnected)
            {
                Log.Warning("Can't Send packet, not connected!");
                return;
            }
            else
            {
                _toSendPackets.Enqueue(packet);
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
        /// Send a disconnect message to the server and destroy our side of the client.
        /// </summary>
        /// <param name="message">
        /// A <see cref="string"/> which shows what message to use to send to the client.
        /// </param>
        public void Disconnect(string message)
        {
            ConnectionUpdatePacket connectionUpdatePacket = new ConnectionUpdatePacket
            {
                State = ConnectionState.Disconnected,
                Reason = message
            };
            Send(connectionUpdatePacket);
            ClientDisconnected.Invoke();
            if (CurrnetClientLocation == ClientLocation.Remote)
            {
                Log.Info($"Disconnecting Client {ClientID} for " + message);
                StopClient();
            }
            if(CurrnetClientLocation == ClientLocation.Local)
            {
                Log.Info("Disconnecting from server. Reason: " + message);
                StopClient();
            }
        }

        /// <summary>
        /// Stops the client, removing the Thread and closing the socket
        /// </summary>
        public void StopClient()
        {
            _connectionState = ConnectionState.Disconnected;
            if(CurrnetClientLocation == ClientLocation.Remote)
            {
                NetworkServer.RemoveClient(ClientID);
            }
            _shuttingDown = true;
            _packetReaderThread.Abort();
            _packetSenderThread.Abort();
            _packetReaderThread = null;
            _packetSenderThread = null;
            _tcpClient = null;
        }
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
}
