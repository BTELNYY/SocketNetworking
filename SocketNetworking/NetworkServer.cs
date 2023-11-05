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
        private static ServerState _serverState = ServerState.NotStarted;

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

        private bool _isShuttingDown = false;

        public static Dictionary<int, NetworkClient> Clients = new Dictionary<int, NetworkClient>();

        public static void StartServer()
        {
            if(ServerStarted)
            {
                Log.Error("Server already started!");
                return;
            }
            NetworkServer server = new NetworkServer();
            _serverInstance = server;
            ServerThread = new Thread(server.ServerStartThread);
            ServerThread.Start();
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
            TcpListener serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), 7777);
            TcpClient clientSocket = default;
            serverSocket.Start();
            Log.Info("Socket Started.");
            int counter = 0;
            while (true)
            {
                if (_isShuttingDown)
                {
                    break;
                }
                counter += 1;
                clientSocket = serverSocket.AcceptTcpClient();
                IPEndPoint remoteIpEndPoint = clientSocket.Client.RemoteEndPoint as IPEndPoint;
                Log.Info($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
                NetworkClient client = new NetworkClient(counter, clientSocket);
                Clients.Add(counter, client);
                CallbackTimer<NetworkClient> callback = new CallbackTimer<NetworkClient>((x) =>
                {
                    if(x.CurrentConnectionState != PacketSystem.ConnectionState.Connected)
                    {
                        x.Disconnect("Failed to handshake in time.");
                    }
                }, client, HandshakeTime);
                callback.Start();
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
