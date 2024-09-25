using System;
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

        private bool _ready = false;

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

        /// <summary>
        /// Should be called locally to initialize the client, switching it from just being created to being ready to be used.
        /// </summary>
        public virtual void InitLocalClient()
        {
            _clientLocation = ClientLocation.Local;
            ClientConnected += OnLocalClientConnected;
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

        /// <summary>
        /// Disconnects the connection with the reason "Disconnected"
        /// </summary>
        public virtual void Disconnect()
        {
            Disconnect("Disconnected");
        }

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

        public virtual void Connect(string host, int port, string password)
        {
            throw new NotImplementedException();
        }

        public virtual byte[] Recieve()
        {
            throw new NotImplementedException();
        }

        public virtual void Send(byte[] data)
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
