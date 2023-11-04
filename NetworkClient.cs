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

namespace SocketNetworking
{
    public class NetworkClient
    {
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

        private bool _clientActive = false;

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

        private Dictionary<int, Type> AdditionalPacketTypes = new Dictionary<int, Type>();

        /// <summary>
        /// Scans the provided assembly for all types with the <see cref="PacketDefinition"/> Attribute, then loads them into a dictionary so that the library can call methods on your netowrk objects.
        /// </summary>
        /// <param name="assmebly">
        /// The <see cref="Assembly"/> which to scan.
        /// </param>
        /// <exception cref="CustomPacketCollisionException">
        /// Thrown when 2 or more packets collide by attempting to register themselves on the same PacketID.
        /// </exception>
        public void ImportCustomPackets(Assembly assmebly)
        {
            List<Type> types = assmebly.GetTypes().Where(x => x.IsSubclassOf(typeof(CustomPacket))).ToList();
            types = types.Where(x => x.GetCustomAttribute(typeof(PacketDefinition)) != null).ToList();
            foreach(Type type in types)
            {
                CustomPacket packet = (CustomPacket)Activator.CreateInstance(type);
                int customPacketId = packet.CustomPacketID;
                if (AdditionalPacketTypes.ContainsKey(customPacketId))
                {
                    throw new CustomPacketCollisionException(customPacketId, AdditionalPacketTypes[customPacketId], type);
                }
                Log.Info($"Adding custom packet with ID {customPacketId} and name {type.Name}");
                AdditionalPacketTypes.Add(customPacketId, type);
            }
        }

        /// <summary>
        /// Adds a list of custom packets to the additional packets dictionary. Note that all packets provided must be instances and they must all have the <see cref="PacketDefinition"/> attribute.
        /// </summary>
        /// <param name="packets">
        /// List of <see cref="CustomPacket"/>s to add.
        /// </param>
        /// <exception cref="CustomPacketCollisionException"></exception>
        public void ImportCustomPackets(List<CustomPacket> packets)
        {
            foreach(CustomPacket packet in packets)
            {
                if(packet.GetType().GetCustomAttribute(typeof(PacketDefinition)) == null) 
                {
                    Log.Warning($"Custom packet {packet.GetType().Name} does not implement attribute {nameof(PacketDefinition)} it will be ignored.");
                    continue;
                }
                if (AdditionalPacketTypes.ContainsKey(packet.CustomPacketID))
                {
                    throw new CustomPacketCollisionException(packet.CustomPacketID, AdditionalPacketTypes[packet.CustomPacketID], packet.GetType());
                }
                Log.Info($"Adding custom packet with ID {packet.CustomPacketID} and name {packet.GetType().Name}");
                AdditionalPacketTypes.Add(packet.CustomPacketID, packet.GetType());
            }
        }

        /// <summary>
        /// Adds packets from type list to the additional packets dictionary. Note that all packets must inherit from <see cref="CustomPacket"/> and have the <see cref="PacketDefinition"/> attribute.
        /// </summary>
        /// <param name="importedTypes">
        /// List of <see cref="Type"/>s which meet criteria (Inherit from <see cref="CustomPacket"/> and have the <see cref="PacketDefinition"/> attribute) to add.
        /// </param>
        /// <exception cref="CustomPacketCollisionException"></exception>
        public void ImportCustomPackets(List<Type> importedTypes)
        {
            List<Type> types = importedTypes.Where(x => x.IsSubclassOf(typeof(CustomPacket))).ToList();
            types = types.Where(x => x.GetCustomAttribute(typeof(PacketDefinition)) != null).ToList();
            foreach (Type type in types)
            {
                CustomPacket packet = (CustomPacket)Activator.CreateInstance(type);
                int customPacketId = packet.CustomPacketID;
                if (AdditionalPacketTypes.ContainsKey(customPacketId))
                {
                    throw new CustomPacketCollisionException(customPacketId, AdditionalPacketTypes[customPacketId], type);
                }
                Log.Info($"Adding custom packet with ID {customPacketId} and name {type.Name}");
                AdditionalPacketTypes.Add(customPacketId, type);
            }
        }


        private List<INetworkObject> RemoteObjects = new List<INetworkObject>();

        private List<INetworkObject> LocalObjects = new List<INetworkObject>();

        /// <summary>
        /// Adds a <see cref="INetworkObject"/> to the list of objects which we check the methods of for the <see cref="PacketListener"/> attribute. This automatically checks for location of the client, remote or local and adds it as needed.
        /// </summary>
        /// <param name="networkObject">
        /// An instance of the a class which implements the <see cref="INetworkObject"/> interface
        /// </param>
        public void AddNetworkObject(INetworkObject networkObject)
        {
            if(CurrnetClientLocation == ClientLocation.Local)
            {
                LocalObjects.Add(networkObject);
            }
            if(CurrnetClientLocation == ClientLocation.Remote)
            {
                RemoteObjects.Add(networkObject);
            }
        }

        private bool _shuttingDown = false;

        /// <summary>
        /// Local Client start, at this point we will listen for packets coming from the server.
        /// </summary>
        public NetworkClient()
        {
            _clientLocation = ClientLocation.Local;
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
        /// <returns>
        /// A <see cref="bool"/> indicating connection success. Note this only returns the status of the socket connection, not of the full connection action. E.g. you can still fail to connect if the server refuses to accept the client.
        /// </returns>
        public bool Connect(string hostname, int port)
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
            try
            {
                _tcpClient.Connect(hostname, port);
            }
            catch(Exception ex)
            {
                Log.Error($"Failed to connect: \n {ex}");
                return false;
            }
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
            _clientThread = new Thread(ClientStartThread);
            _clientThread.Start();
            _clientActive = true;
        }

        /// <summary>
        /// Used when creating a <see cref="NetworkClient"/> object on the server. Do not call this on the client.
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
            _clientLocation = ClientLocation.Remote;
            _clientThread = new Thread(ClientStartThread);
            _clientThread.Start();
        }

        private void ClientStartThread()
        {
            Log.Info($"Client thread started, ID {ClientID}");
            while (true)
            {
                if (_shuttingDown)
                {
                    break;
                }
                if (!IsConnected)
                {
                    continue;
                }
                if(TcpClient.ReceiveBufferSize == 0)
                {
                    continue;
                }
                byte[] buffer = new byte[TcpClient.ReceiveBufferSize];
                int count = NetworkStream.Read(buffer, 0, TcpClient.ReceiveBufferSize);
                Log.Debug($"Reading Incoming Packet. Size: {count}");
                PacketHeader header = Packet.ReadPacketHeader(buffer);
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
            List<INetworkObject> objects = RemoteObjects.Where(x => x.NetworkID == header.NetworkIDTarget).ToList();
            if (!AdditionalPacketTypes.ContainsKey(header.CustomPacketID))
            {
                Log.Error("Unknown Custom packet. ID: " + header.CustomPacketID);
                return;
            }
            Type packetType = AdditionalPacketTypes[header.CustomPacketID];
            Packet packet = (Packet)Activator.CreateInstance(AdditionalPacketTypes[header.CustomPacketID]);
            packet.Deserialize(data);
            List<MethodInfo> methods = new List<MethodInfo>();
            //This may look not very effecient, but you arent checking EVERY possible object, only the ones which match the TargetID.
            //The other way I could do this is by making a nested dictionary hell hole, but I dont want to do that.
            foreach (INetworkObject netObj in objects)
            {
                Type typeOfObject = netObj.GetType();
                MethodInfo[] allPacketListeners = typeOfObject.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.GetCustomAttribute(typeof(PacketListener)) != null).ToArray();
                List<MethodInfo> validMethods = new List<MethodInfo>();
                foreach (MethodInfo method in allPacketListeners)
                {
                    PacketListener listener = (PacketListener)method.GetCustomAttribute(typeof(PacketListener));
                    if (listener.DefinedType != packetType)
                    {
                        continue;
                    }
                    if (listener.DefinedDirection == PacketDirection.Any)
                    {
                        validMethods.Add(method);
                        continue;
                    }
                    if (listener.DefinedDirection == PacketDirection.Client && CurrnetClientLocation != ClientLocation.Remote)
                    {
                        continue;
                    }
                    if (listener.DefinedDirection == PacketDirection.Server && CurrnetClientLocation != ClientLocation.Local)
                    {
                        continue;
                    }
                    validMethods.Add(method);
                }
                foreach (MethodInfo method in validMethods)
                {
                    method.Invoke(netObj, new object[] { packet });
                }
            }
        }


        /// <summary>
        /// Only called on the client.
        /// </summary>
        private void HandleLocalClient(PacketHeader header, byte[] data)
        {
            List<INetworkObject> objects = LocalObjects.Where(x => x.NetworkID == header.NetworkIDTarget).ToList();
            if (!AdditionalPacketTypes.ContainsKey(header.CustomPacketID))
            {
                Log.Error("Unknown Custom packet. ID: " + header.CustomPacketID);
                return;
            }
            Type packetType = AdditionalPacketTypes[header.CustomPacketID];
            Packet packet = (Packet)Activator.CreateInstance(AdditionalPacketTypes[header.CustomPacketID]);
            packet.Deserialize(data);
            List<MethodInfo> methods = new List<MethodInfo>();
            //This may look not very effecient, but you arent checking EVERY possible object, only the ones which match the TargetID.
            //The other way I could do this is by making a nested dictionary hell hole, but I dont want to do that.
            foreach(INetworkObject netObj in objects)
            {
                Type typeOfObject = netObj.GetType();
                MethodInfo[] allPacketListeners = typeOfObject.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.GetCustomAttribute(typeof(PacketListener)) != null).ToArray();
                List<MethodInfo> validMethods = new List<MethodInfo>();
                foreach (MethodInfo method in allPacketListeners)
                {
                    PacketListener listener = (PacketListener)method.GetCustomAttribute(typeof(PacketListener));
                    if (listener.DefinedType != packetType)
                    {
                        continue;
                    }
                    if (listener.DefinedDirection == PacketDirection.Any)
                    {
                        validMethods.Add(method);
                        continue;
                    }
                    if (listener.DefinedDirection == PacketDirection.Client && CurrnetClientLocation != ClientLocation.Remote)
                    {
                        continue;
                    }
                    if (listener.DefinedDirection == PacketDirection.Server && CurrnetClientLocation != ClientLocation.Local)
                    {
                        continue;
                    }
                    validMethods.Add(method);
                }
                foreach(MethodInfo method in validMethods)
                {
                    method.Invoke(netObj, new object[] { packet });
                }
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
            }
            else
            {
                NetworkStream serverStream = NetworkStream;
                byte[] packetBytes = packet.Serialize().Data;
                serverStream.Write(packetBytes, 0, packetBytes.Length);
                serverStream.Flush();
            }
        }
    }
}
