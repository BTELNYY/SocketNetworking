﻿using SocketNetworking.PacketSystem;
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

namespace SocketNetworking
{
    public class NetworkClient
    {
        /// <summary>
        /// Called on both Remote and Local clients when the connection has succeeded and the Socket is ready to use.
        /// </summary>
        public Action ClientConnected;
        /// <summary>
        /// Called on both Remote and Local Clients when the connection state changes.
        /// </summary>
        public Action<ConnectionState> ConnectionStateUpdated;

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
                if(CurrnetClientLocation != ClientLocation.Remote)
                {
                    Log.Warning("Local Client tired modifying its own ready state.");
                    return;
                }
                if(!IsConnected || CurrentConnectionState != ConnectionState.Connected) 
                {
                    Log.Warning("Can't change ready state becuase the socket is not connected or the handshake isn't done.");
                    return;
                }
                ReadyStateUpdatePacket readyStateUpdatePacket = new ReadyStateUpdatePacket();
                readyStateUpdatePacket.Ready = value;
                _ready = value;
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
                ConnectionUpdatePacket updatePacket = new ConnectionUpdatePacket();
                updatePacket.State = value;
                updatePacket.Reason = "Setter in remote.";
                Send(updatePacket);
                _connectionState = value;
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

        private Thread _clientThread;

        /// <summary>
        /// The Clients <see cref="Thread"/> which handles all packets
        /// </summary>
        public Thread ClientThread
        {
            get
            {
                return _clientThread;
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
        /// Local Client start, at this point we will listen for packets coming from the server.
        /// </summary>
        public NetworkClient()
        {
            _clientLocation = ClientLocation.Local;
            ClientConnected += OnLocalClientConnected;
        }

        /// <summary>
        /// Used when creating a <see cref="NetworkClient"/> object on the server. Do not call this on the local client.
        /// </summary>
        /// <param name="clientId">
        /// Given ClientID
        /// </param>
        /// <param name="socket">
        /// The <see cref="System.Net.Sockets.TcpClient"/> object which handles data transport.
        /// </param>
        public NetworkClient(int clientId, TcpClient socket)
        {
            _clientId = clientId;
            _tcpClient = socket;
            _tcpClient.NoDelay = true;
            _clientLocation = ClientLocation.Remote;
            ClientConnected += OnRemoteClientConnected;
            ClientConnected?.Invoke();
            _clientThread = new Thread(ClientStartThread);
            _clientThread.Start();
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
            _tcpClient.NoDelay = true;
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
            if (_clientThread != null)
            {
                _clientThread.Abort();
            }
            _clientThread = new Thread(ClientStartThread);
            _clientActive = true;
            _shuttingDown = false;
            _clientThread.Start();
            ClientConnected?.Invoke();
        }



        private void ClientStartThread()
        {
            Log.Info($"Client thread started, ID {ClientID}");
            _tcpClient.NoDelay = true;
            while (true)
            {
                if (_shuttingDown)
                {
                    Log.Info("Shutting down loop");
                    break;
                }
                if (!IsConnected)
                {
                    Log.Debug("Not connected!");
                    continue;
                }
                if(TcpClient.ReceiveBufferSize == 0)
                {
                    continue;
                }
                if (!NetworkStream.DataAvailable)
                {
                    //Log.Debug("Nothing to read on stream");
                    continue;
                }
                byte[] buffer = new byte[Packet.MaxPacketSize];
                int count = NetworkStream.Read(buffer, 0, Packet.MaxPacketSize);
                PacketHeader header = Packet.ReadPacketHeader(buffer);
                Log.Debug($"Inbound Packet Info, Size: {count}, Type: {header.Type}, Target: {header.NetworkIDTarget}, CustomPacketID: {header.CustomPacketID}");
                if (CurrnetClientLocation == ClientLocation.Remote)
                {
                    HandleRemoteClient(header, buffer);
                }
                if(CurrnetClientLocation == ClientLocation.Local)
                {
                    HandleLocalClient(header, buffer);
                }
            }
            Log.Info("Shutting down client, Closing socket.");
            _tcpClient.Close();
        }

        /// <summary>
        /// Only called on the server.
        /// </summary>
        private void HandleRemoteClient(PacketHeader header, byte[] data)
        {
            switch (header.Type)
            {
                case PacketType.CustomPacket:
                    NetworkManager.TriggerPacketListeners(header, data, CurrnetClientLocation);
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
                    ServerDataPacket serverDataPacket = new ServerDataPacket();
                    serverDataPacket.YourClientID = _clientId;
                    serverDataPacket.Configuration = NetworkServer.ServerConfiguration;
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
                    NetworkManager.TriggerPacketListeners(header, data, CurrnetClientLocation);
                    break;
                case PacketType.ReadyStateUpdate:
                    ReadyStateUpdatePacket readyStateUpdatePacket = new ReadyStateUpdatePacket();
                    readyStateUpdatePacket.Deserialize(data);
                    _ready = readyStateUpdatePacket.Ready;
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
                    break;
                default:
                    Log.Error("Packet is not handled!");
                    break;
            }
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
                Log.Info("Sending packet. Type: " + packet.Type.ToString());
                NetworkStream serverStream = NetworkStream;
                byte[] packetBytes = packet.Serialize().Data;
                if(packetBytes.Length > Packet.MaxPacketSize)
                {
                    Log.Error("Packet too large!");
                    return;
                }
                serverStream.Write(packetBytes, 0, packetBytes.Length);
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
            ConnectionUpdatePacket connectionUpdatePacket = new ConnectionUpdatePacket();
            connectionUpdatePacket.State = ConnectionState.Disconnected;
            connectionUpdatePacket.Reason = message;
            Send(connectionUpdatePacket);
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
                NetworkServer.Clients.Remove(ClientID);
            }
            _shuttingDown = true;
            _clientThread.Abort();
            _clientThread = null;
            _tcpClient = null;
        }
    }
}
