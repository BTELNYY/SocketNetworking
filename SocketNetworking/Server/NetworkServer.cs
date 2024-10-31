using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem;
using SocketNetworking.Misc;
using SocketNetworking.Transports;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace SocketNetworking.Server
{
    public class NetworkServer
    {
        public static event Action ServerReady;

        protected static void InvokeServerReady()
        {
            ServerReady?.Invoke();
        }

        public static event Action<int> ClientConnected;

        protected static void InvokeClientConnected(int id)
        {
            ClientConnected?.Invoke(id);
        }

        public static event Action<int> ClientDisconnected;

        protected static void InvokeClientDisconnected(int id)
        {
            ClientDisconnected?.Invoke(id);
        }

        public static event Action ServerStarted;

        protected static void InvokeServerStarted()
        {
            ServerStarted?.Invoke();
        }

        public static event Action ServerStopped;

        protected static void InvokeServerStopped()
        {
            ServerStopped?.Invoke();
        }

        protected static ServerState _serverState = ServerState.NotStarted;

        public static ServerState CurrentServerState 
        { 
            get
            {
                return _serverState;
            }
        } 

        public static bool HasServerStarted
        {
            get
            {
                List<ServerState> states = new List<ServerState>() { ServerState.Ready, ServerState.Started, ServerState.NotReady };
                return states.Contains(CurrentServerState);
            }
        }

        public static bool HasClients
        {
            get
            {
                return _clients.Count > 0;
            }
        }

        public static List<NetworkClient> ConnectedClients
        {
            get
            {
                List<NetworkClient> clients = _clients.Values.ToList();
                return clients.Where(x => x.IsTransportConnected).ToList();
            }
        }

        public static List<NetworkClient> Clients
        {
            get
            {
                return _clients.Values.ToList();
            }
        }

        public static bool Active
        {
            get
            {
                return CurrentServerState == ServerState.Ready;
            }
        }

        private static ProtocolConfiguration _serverConfig = new ProtocolConfiguration();

        public static ProtocolConfiguration ServerConfiguration
        {
            get
            {
                return _serverConfig;
            }
            set
            {
                if (HasServerStarted)
                {
                    Log.GlobalError("Can't change server network configuration is the server is running. Stop it first.");
                    return;
                }
                _serverConfig = value;
            }
        }

        /// <summary>
        /// The Servers <see cref="ServerEncryptionMode"/>.
        /// </summary>
        public static ServerEncryptionMode EncryptionMode = ServerEncryptionMode.Request;

        /// <summary>
        /// Should the server accept the connection, then instantly disconnect the client with a message?
        /// </summary>
        public static bool AutoDisconnectClients = false;

        /// <summary>
        /// Message to auto disconnect clients with
        /// </summary>
        public static string AutoDisconnectMessage = "Server is not ready!";

        /// <summary>
        /// Should the server be currently accepting connections? if this is set to false, the server will not reply to socket requests.
        /// </summary>
        public static bool ShouldAcceptConnections = true;

        /// <summary>
        /// How long should the server wait for the client to complete the handshake?
        /// </summary>
        public static float HandshakeTime = 10f;

        /// <summary>
        /// The server password, this will never be sent accross the network.
        /// </summary>
        public static string ServerPassword = "default";

        /// <summary>
        /// Should the server check for client passwords?
        /// </summary>
        public static bool UseServerPassword = false;

        /// <summary>
        /// What port should the server start on?
        /// </summary>
        public static int Port = 7777;

        /// <summary>
        /// What IP should the server bind to?
        /// </summary>
        public static string BindIP = "0.0.0.0";

        /// <summary>
        /// Should the server Auto-Ready Clients when the the <see cref="NetworkClient.CurrentConnectionState"/> becomes <see cref="ConnectionState.Connected"/>?
        /// </summary>
        public static bool DefaultReady = true;

        /// <summary>
        /// What type should network clients be created in? Note that the type must inherit from <see cref="NetworkClient"/>. This type should not have any class constructors.
        /// </summary>
        public static Type ClientType = typeof(NetworkClient);

        /// <summary>
        /// Should clients be able to set themselves as ready using <see cref="SocketNetworking.PacketSystem.Packets.ReadyStateUpdatePacket"/>?
        /// </summary>
        public static bool AllowClientSelfReady = true;

        public static Thread ServerThread;

        public static Thread ServerUdpThread;

        private static NetworkServer _serverInstance;

        public static NetworkServer ServerInstance
        {
            get
            {
                if(_serverInstance == null)
                {
                    Log.GlobalError("Tried to get stopped server.");
                    throw new InvalidOperationException("Attempted to access server instance when it isn't running.");
                }
                else
                {
                    return _serverInstance;
                }
            }
        }

        protected readonly bool _isShuttingDown = false;

        private static readonly Dictionary<int, NetworkClient> _clients = new Dictionary<int, NetworkClient>();


        public void StartServer()
        {
            if (HasServerStarted)
            {
                Log.GlobalError("Server already started!");
                return;
            }
            _serverState = ServerState.Started;
            NetworkServer server = GetServer();
            if (!server.ValidateClient())
            {
                return;
            }
            _serverInstance = server;
            ServerThread = new Thread(server.ServerStartThread);
            ServerThread.Start();
            ServerStarted?.Invoke();
            _serverState = ServerState.NotReady;
        }

        protected virtual bool ValidateClient()
        {
            if (!ClientType.IsSubclassOf(typeof(NetworkClient)))
            {
                Log.GlobalError("Can't start server: Client Type is not correct. Should be a subclass of NetworkClient");
                return false;
            }
            return true;
        }

        protected virtual NetworkServer GetServer()
        {
            return new NetworkServer();
        }

        protected static void AddClient(NetworkClient client, int clientId)
        {
            if (_clients.ContainsKey(clientId))
            {
                Log.GlobalError("Something really got fucked up!");
                //we throw becuase the whole server will die if we cant add the client.
                throw new InvalidOperationException("Client ID to add already taken!");
            }
            else
            {
                NetworkClient cursedClient = (NetworkClient)Convert.ChangeType(client, ClientType);
                _clients.Add(clientId, cursedClient);
                Log.GlobalDebug($"Added client. ID: {clientId}, Type: {cursedClient.GetType().FullName}");
            }
        }

        public static void RemoveClient(int clientId)
        {
            if (_clients.ContainsKey(clientId))
            {
                ClientDisconnected?.Invoke(clientId);
                _clients.Remove(clientId);
            }
            else
            {
                Log.GlobalWarning("Can't remove client: ID not found.");
            }
        }

        /// <summary>
        /// This method will return a client if possible. Or <see cref="null"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns>
        /// A type casted instance of <see cref="NetworkClient"/>. You can always get your client type by casting it again.
        /// </returns>
        public static NetworkClient GetClient(int id)
        {
            if (_clients.ContainsKey(id))
            {
                return _clients[id];
            }
            else
            {
                return null;
            }
        }

        public static void StopServer()
        {
            if (!HasServerStarted)
            {
                Log.GlobalError("Server already stopped.");
            }
            foreach(NetworkClient client in _clients.Values)
            {
                client.Disconnect("Server shutting down");
            }
            _clients.Clear();
            ServerThread.Abort();
            ServerStopped?.Invoke();
        }

        protected virtual void ServerStartThread()
        {
            throw new NotImplementedException("Trying to use the non-overriden server thread, you should probably override it, do not run the base method!");
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to all connected clients.
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> to send.
        /// </param>
        /// <param name="toReadyOnly">
        /// if this is true, only send to <see cref="NetworkClient.Ready"/> clients, otherwise send to everyone.
        /// </param>
        public static void SendToAll(Packet packet, bool toReadyOnly = false)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            if (toReadyOnly)
            {
                SendToReady(packet);
                return;
            }
            foreach(NetworkClient client in _clients.Values)
            {
                client.Send(packet);
            }
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to all connected clients.
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> to send.
        /// </param>
        /// <param name="toReadyOnly">
        /// if this is true, only send to <see cref="NetworkClient.Ready"/> clients, otherwise send to everyone.
        /// </param>
        /// <param name="target">
        /// The <see cref="INetworkObject"/> which is the target.
        /// </param>
        public static void SendToAll(Packet packet, INetworkObject target, bool toReadyOnly = false)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            if (toReadyOnly)
            {
                SendToReady(packet);
                return;
            }
            foreach (NetworkClient client in _clients.Values)
            {
                client.Send(packet, target);
            }
        }


        /// <summary>
        /// Disconnects all clients who aren't ready.
        /// </summary>
        /// <param name="reason">
        /// The Disconnect reason.
        /// </param>
        public static void DisconnectNotReady(string reason = "Failed to ready in time.")
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            List<NetworkClient> readyClients = _clients.Values.Where(x => !x.Ready).ToList();
            foreach (NetworkClient client in readyClients)
            {
                client.Disconnect(reason);
            }
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to all ready clients
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> to send.
        /// </param>
        static void SendToReady(Packet packet)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            List<NetworkClient> readyClients = _clients.Values.Where(x => x.Ready).ToList();
            foreach(NetworkClient client in readyClients)
            {
                client.Send(packet);
            }
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to all ready clients
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> to send.
        /// </param>
        /// <param name="target">
        /// The <see cref="INetworkObject"/> which is the target.
        /// </param>
        static void SendToReady(Packet packet, INetworkObject target)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            List<NetworkClient> readyClients = _clients.Values.Where(x => x.Ready).ToList();
            foreach (NetworkClient client in readyClients)
            {
                client.Send(packet, target);
            }
        }

        public static void NetworkInvokeOnAll(object obj, string methodName, object[] args, bool readyOnly = false)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            List<NetworkClient> clients = _clients.Values.ToList();
            if(readyOnly)
            {
                clients = clients.Where(x => x.Ready).ToList();
            }
            foreach (NetworkClient client in clients)
            {
                client.NetworkInvoke(obj, methodName, args);
            }
        }
    }

    public enum ServerState 
    {
        NotStarted,
        Started,
        NotReady,
        Ready,
    }

    public enum ServerEncryptionMode
    {
        /// <summary>
        /// Encryption is not handled at all by the server.
        /// </summary>
        Disabled,
        /// <summary>
        /// Encryption is only enabled if the client requests it via <see cref="NetworkClient.ClientRequestEncryption"/>
        /// </summary>
        Request,
        /// <summary>
        /// Encryption is required.
        /// </summary>
        Required
    }
}
