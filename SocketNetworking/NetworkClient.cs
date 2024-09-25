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

namespace SocketNetworking
{
    public class NetworkClient : NetworkClientBase
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
            networkEncryptionManager = new NetworkEncryptionManager();
            Init();
        }

        public override void Init()
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
        /// Called when the state of the <see cref="NetworkClient.Ready"/> variable changes. First variable is the old state, and the second is the new state. This event is fired on both Local and Remote clients.
        /// </summary>
        public event Action<bool, bool> ReadyStateChanged;

        /// <summary>
        /// Called on Local and Remote clients when a full packet is read.
        /// </summary>
        public event Action<PacketHeader, byte[]> PacketRead;

        /// <summary>
        /// Called when a packet is ready to handle, this event is never called if <see cref="NetworkClient.ManualPacketHandle"/> is set to false
        /// </summary>
        public event Action<PacketHeader, byte[]> PacketReadyToHandle;

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

        private NetworkEncryptionManager networkEncryptionManager;

        public NetworkEncryptionManager EncryptionManager
        {
            get
            {
                return networkEncryptionManager;
            }
        }

        /// <summary>
        /// Returns the Address to which the socket is connected too, In the format IP:Port
        /// </summary>
        public override string ConnectedIPAndPort
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
        public override string ConnectedIP
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
        public override int ConnectedPort
        {
            get
            {
                IPEndPoint remoteIpEndPoint = TcpClient.Client.RemoteEndPoint as IPEndPoint;
                return remoteIpEndPoint.Port;
            }
        }

        /// <summary>
        /// The <see cref="System.Net.Sockets.TcpClient"/>s <see cref="System.Net.Sockets.NetworkStream"/>
        /// </summary>
        public NetworkStream NetworkStream
        {
            get
            {
                if (IsConnected)
                {
                    return TcpClient.GetStream();
                }
                return null;
            }
        }

        private bool _tcpNoDelay = false;

        /// <summary>
        /// Sets if the TCP Socket should wait for more packets before sending.
        /// </summary>
        public bool TcpNoDelay
        {
            get
            {
                return _tcpNoDelay;
            }
            set
            {
                if(TcpClient != null)
                {
                    TcpClient.NoDelay = value;
                }
                _tcpNoDelay = value;
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
        public override bool IsConnected
        {
            get
            {
                if(TcpClient == null)
                {
                    return false;
                }
                if(CurrnetClientLocation == ClientLocation.Remote && TcpClient.Connected)
                {
                    return true;
                }
                return TcpClient != null && TcpClient.Connected;
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
                if (IsConnected)
                {
                    Log.GlobalError("Can't update NetworkConfiguration while client is connected.");
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
        public override void InitRemoteClient(int clientId, object socket)
        {
            if(!(socket is TcpClient client))
            {
                throw new ArgumentException("Client is invalid!!");
            }
            _tcpClient = client;
            _tcpClient.NoDelay = TcpNoDelay;

        }

        /// <summary>
        /// Should be called locally to initialize the client, switching it from just being created to being ready to be used.
        /// </summary>
        public override void InitLocalClient()
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
        public override bool Connect(string hostname, int port, string password)
        {
            if(CurrnetClientLocation == ClientLocation.Remote)
            {
                Log.GlobalError("Cannot connect to other servers from remote.");
                return false;
            }
            if (IsConnected)
            {
                Log.GlobalError("Can't connect: Already connected to a server.");
                return false;
            }
            _tcpClient = new TcpClient();
            _tcpClient.NoDelay = TcpNoDelay;
            try
            {
                _tcpClient.Connect(hostname, port);
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
        /// Sends any <see cref="Packet"/> down the network stream to whatever is connected on the other side. Note that this method doesn't check who it is sending it to, instead sending it to the current stream.
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> to send down the stream.
        /// </param>
        public override void Send(Packet packet)
        {

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
        /// Stops the client, removing the Thread and closing the socket
        /// </summary>
        public override void StopClient()
        {
            _tcpClient = null;
        }

        protected override void OnLocalClientConnected()
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


        object streamLock = new object();

        internal void SendNextPacketInternal()
        {
            if (_toSendPackets.IsEmpty)
            {
                return;
            }
            lock (streamLock)
            {
                _toSendPackets.TryDequeue(out Packet packet);
                if(!packet.ValidateFlags())
                {
                    Log.GlobalError($"Packet send failed! Flag validation failure. Packet Type: {packet.Type}, Target: {packet.NetowrkIDTarget}, Custom Packet ID: {packet.CustomPacketID}, Active Flags: {string.Join(", ", packet.Flags.GetActiveFlags())}");
                    return;
                }
                NetworkStream serverStream = NetworkStream;
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
                    if(currentEncryptionState < (int)EncryptionState.AsymmetricalReady)
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
                try
                {
                    Log.GlobalDebug($"Sending packet. Target: {packet.NetowrkIDTarget} Type: {packet.Type} CustomID: {packet.CustomPacketID} Length: {fullBytes.Length}");
                    serverStream.Write(fullBytes, 0, fullBytes.Length);
                    //LMAO, I DONT KNOW WHY I NEED THIS!
                    //Do not remove, will break if removed. (Don't question it)
                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    Log.GlobalError("Failed to send packet! Error:\n" + ex.ToString());
                    NetworkErrorData networkErrorData = new NetworkErrorData("Failed to send packet: " + ex.ToString(), true);
                    ConnectionError?.Invoke(networkErrorData);
                }
            }
        }

        void PacketReaderThreadMethod()
        {
            Log.GlobalInfo($"Client thread started, ID {ClientID}");
            //int waitingSize = 0;
            //byte[] prevPacketFragment = { };
            byte[] buffer = new byte[Packet.MaxPacketSize]; // this can now be freely changed
            int fillSize = 0; // the amount of bytes in the buffer. Reading anything from fillsize on from the buffer is undefined.
            while (true)
            {
            Packet: // this is for breaking a nested loop further down. thanks C#
                if (_shuttingDown)
                {
                    Log.GlobalInfo("Shutting down loop");
                    break;
                }
                if (!IsConnected)
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
                        if (TcpNoDelay)
                        {
                            count = NetworkStream.Read(buffer, 0, buffer.Length - fillSize);
                        }
                        else
                        {
                            count = NetworkStream.Read(buffer, fillSize, buffer.Length - fillSize);
                        }
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
                        count = NetworkStream.Read(buffer, fillSize, buffer.Length - fillSize);
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
                PacketHeader header = Packet.ReadPacketHeader(fullPacket);
                if(header.Type == PacketType.CustomPacket && NetworkManager.GetCustomPacketByID(header.CustomPacketID) == null)
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
                if (header.Flags.HasFlag(PacketFlags.AsymtreicalEncrypted))
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
                // any leftover data in the buffer is recycled for the next iteration 
                //unless we don't do a delay, becuase there is no point in keeping the buffer.
                if (TcpNoDelay)
                {
                    buffer = new byte[Packet.MaxPacketSize];
                    fillSize = 0;
                }
            }
            Log.GlobalInfo("Shutting down client, Closing socket.");
            _tcpClient.Close();
        }

        ConcurrentQueue<ReadPacketInfo> _toReadPackets = new ConcurrentQueue<ReadPacketInfo>();

        void HandlePacket(PacketHeader header, byte[] fullPacket)
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
    }
}
