using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketNetworking
{
    public class NetworkServer
    {
        public static event Action ServerReady;
        public static event Action<int> ClientConnected;
        public static event Action<int> ClientDisconnected;

        private static readonly ServerState _serverState = ServerState.NotStarted;

        public static ServerState CurrentServerState 
        { 
            get
            {
                return _serverState;
            }
        } 

        public static bool ServerStarted
        {
            get
            {
                List<ServerState> states = new List<ServerState>() { ServerState.Ready, ServerState.Started, ServerState.NotReady };
                return states.Contains(CurrentServerState);
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
                if (ServerStarted)
                {
                    Log.Error("Can't change server network configuration is the server is running. Stop it first.");
                    return;
                }
                _serverConfig = value;
            }
        }

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

        private static NetworkServer _serverInstance;

        public static NetworkServer ServerInstance
        {
            get
            {
                if(_serverInstance == null)
                {
                    Log.Error("Tried to get stopped server.");
                    throw new InvalidOperationException("Attempted to access server instance when it isn't running.");
                }
                else
                {
                    return _serverInstance;
                }
            }
        }

        private readonly bool _isShuttingDown = false;

        private static readonly Dictionary<int, NetworkClient> Clients = new Dictionary<int, NetworkClient>();

        public static void StartServer()
        {
            if(ServerStarted)
            {
                Log.Error("Server already started!");
                return;
            }
            if (!ClientType.IsSubclassOf(typeof(NetworkClient)))
            {
                Log.Error("Can't start server: ClientType is not correct.");
                return;
            }
            NetworkServer server = new NetworkServer();
            _serverInstance = server;
            ServerThread = new Thread(server.ServerStartThread);
            ServerThread.Start();
        }

        protected static void AddClient(NetworkClient client, int clientId)
        {
            if (Clients.ContainsKey(clientId))
            {
                Log.Error("Something really got fucked up!");
                //we throw becuase the whole server will die if we cant add the client.
                throw new InvalidOperationException("Client ID to add already taken!");
            }
            else
            {
                //cursed...
                NetworkClient cursedClient = (NetworkClient)Convert.ChangeType(client, ClientType);
                Clients.Add(clientId, cursedClient);
            }
        }

        public static void RemoveClient(int clientId)
        {
            if (Clients.ContainsKey(clientId))
            {
                Clients.Remove(clientId);
                ClientDisconnected.Invoke(clientId);
            }
            else
            {
                Log.Warning("Can't remove client: ID not found.");
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
            if (Clients.ContainsKey(id))
            {
                return Clients[id];
            }
            else
            {
                return null;
            }
        }

        public static void StopServer()
        {
            if (!ServerStarted)
            {
                Log.Error("Server already stopped.");
            }
            foreach(NetworkClient client in Clients.Values)
            {
                client.Disconnect("Server shutting down");
            }
            Clients.Clear();
            ServerThread.Abort();
        }

        private void ServerStartThread()
        {
            Log.Info("Server starting...");
            TcpListener serverSocket = new TcpListener(IPAddress.Parse(BindIP), Port);
            serverSocket.Start();
            Log.Info("Socket Started.");
            Log.Info($"Listening on {BindIP}:{Port}");
            int counter = 0;
            ServerReady?.Invoke();
            while (true)
            {
                if (_isShuttingDown)
                {
                    break;
                }
                TcpClient socket = serverSocket.AcceptTcpClient();
                socket.NoDelay = true;
                IPEndPoint remoteIpEndPoint = socket.Client.RemoteEndPoint as IPEndPoint;
                Log.Info($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
                NetworkClient client = (NetworkClient)Activator.CreateInstance(ClientType);
                client.InitRemoteClient(counter, socket);
                AddClient(client, counter);
                CallbackTimer<NetworkClient> callback = new CallbackTimer<NetworkClient>((x) =>
                {
                    if(x == null)
                    {
                        return;
                    }
                    if(x.CurrentConnectionState != ConnectionState.Connected)
                    {
                        x.Disconnect("Failed to handshake in time.");
                    }
                }, client, HandshakeTime);
                callback.Start();
                ClientConnected?.Invoke(counter);
                counter++;
            }
            Log.Info("Shutting down!");
            serverSocket.Stop();
        }
    }

    public enum ServerState 
    {
        NotStarted,
        Started,
        NotReady,
        Ready,
    }
}
