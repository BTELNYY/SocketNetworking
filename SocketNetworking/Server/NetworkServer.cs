using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Authentication;
using SocketNetworking.Shared.Events;
using SocketNetworking.Shared.Messages;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Transports;

namespace SocketNetworking.Server
{
    /// <summary>
    /// The <see cref="NetworkServer"/> class is the base class for all server implentations.
    /// </summary>
    public class NetworkServer
    {
        public static event Action ServerReady;

        /// <summary>
        /// Invokes the <see cref="ServerReady"/> event.
        /// </summary>
        protected static void InvokeServerReady()
        {
            ServerReady?.Invoke();
        }

        public static event Action<NetworkClient> ClientConnected;

        /// <summary>
        /// Invokes the <see cref="ClientConnected"/> event.
        /// </summary>
        /// <param name="client"></param>
        protected static void InvokeClientConnected(NetworkClient client)
        {
            ClientConnected?.Invoke(client);
        }

        public static event Action<NetworkClient> ClientDisconnected;

        /// <summary>
        /// Invokes the <see cref="ClientDisconnected"/> event.
        /// </summary>
        /// <param name="client"></param>
        protected static void InvokeClientDisconnected(NetworkClient client)
        {
            ClientDisconnected?.Invoke(client);
        }

        public static event Action ServerStarted;

        /// <summary>
        /// Invokes the <see cref="ServerStarted"/> event.
        /// </summary>
        protected static void InvokeServerStarted()
        {
            ServerStarted?.Invoke();
        }

        /// <summary>
        /// Invokes the <see cref="ServerStopped"/> event.
        /// </summary>
        public static event Action ServerStopped;

        protected static void InvokeServerStopped()
        {
            ServerStopped?.Invoke();
        }

        /// <summary>
        /// Called when a <see cref="NetworkClient"/> is first connecting. At this point, this <see cref="NetworkClient"/> is fresh.
        /// </summary>
        public static event EventHandler<ClientConnectRequest> ClientConnecting;

        /// <summary>
        /// Invokes the <see cref="ClientConnecting"/> event and returns the result of the event.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        protected static ClientConnectRequest AcceptClient(NetworkClient client)
        {
            ClientConnectRequest req = new ClientConnectRequest(client, true);
            ClientConnecting?.Invoke(null, req);
            return req;
        }

        protected static ServerState _serverState = ServerState.NotStarted;

        /// <summary>
        /// The Current <see cref="ServerState"/>.
        /// </summary>
        public static ServerState CurrentServerState
        {
            get
            {
                return _serverState;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the <see cref="CurrentServerState"/> is <see cref="ServerState.Ready"/>, <see cref="ServerState.Started"/> or <see cref="ServerState.NotReady"/>.
        /// </summary>
        public static bool HasServerStarted
        {
            get
            {
                List<ServerState> states = new List<ServerState>() { ServerState.Ready, ServerState.Started, ServerState.NotReady };
                return states.Contains(CurrentServerState);
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the <see cref="Clients"/> count is above 0.
        /// </summary>
        public static bool HasClients
        {
            get
            {
                return _clients.Count > 0;
            }
        }

        /// <summary>
        /// Returns the list of <see cref="NetworkClient"/>s which have <see cref="NetworkClient.IsTransportConnected"/> set to <see langword="true"/>.
        /// </summary>
        public static List<NetworkClient> ConnectedClients
        {
            get
            {
                List<NetworkClient> clients = _clients.Values.ToList();
                return clients.Where(x => x.IsTransportConnected).ToList();
            }
        }

        /// <summary>
        /// Returns the list of <see cref="NetworkClient"/>s.
        /// </summary>
        public static List<NetworkClient> Clients
        {
            get
            {
                return _clients.Values.ToList();
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the <see cref="CurrentServerState"/> is <see cref="ServerState.Ready"/>.
        /// </summary>
        public static bool Active
        {
            get
            {
                return CurrentServerState == ServerState.Ready;
            }
        }

        protected static ProtocolConfiguration _serverConfig = new ProtocolConfiguration();

        /// <summary>
        /// The current server <see cref="ProtocolConfiguration"/>. This value cannot be changed if <see cref="HasServerStarted"/> is <see langword="true"/>.
        /// </summary>
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
                    Log.Error("Can't change server network configuration is the server is running. Stop it first.");
                    return;
                }
                _serverConfig = value;
            }
        }

        static Log _log;

        /// <summary>
        /// The server <see cref="SocketNetworking.Log"/>. The Prefix by default is "[Server]".
        /// </summary>
        public static Log Log
        {
            get
            {
                if (_log == null)
                {
                    _log = new Log("[Server]");
                }
                return _log;
            }
        }

        /// <summary>
        /// Internal Server Configuration.
        /// </summary>
        public static NetworkServerConfig Config { get; set; } = new NetworkServerConfig();

        /// <summary>
        /// What type should network clients be created in? Note that the type must inherit from <see cref="NetworkClient"/>. This type should not have any class constructors.
        /// </summary>
        public static Type ClientType
        {
            get => _clientType;
            set
            {
                if (!value.IsSubclassDeep(typeof(NetworkClient)))
                {
                    throw new ArgumentException($"{value.GetType()} is not a subclass of {typeof(NetworkClient)}.");
                }
                _clientType = value;
                return;
            }
        }

        private static Type _clientType = typeof(NetworkClient);


        private static Type _transportType = typeof(NetworkTransport);

        public static Type TransportType
        {
            get => _transportType;
            set
            {
                if (!value.IsSubclassDeep(typeof(NetworkTransport)))
                {
                    throw new ArgumentException($"{value.GetType()} is not a subclass of {typeof(NetworkTransport)}.");
                }
                _transportType = value;
                return;
            }
        }

        /// <summary>
        /// The default <see cref="AuthenticationProvider"/> <see cref="Type"/>. See <see cref="NetworkClient.AuthenticationProvider"/>, <see cref="NetworkClient.Authenticated"/> and <see cref="AuthenticationProvider"/>.
        /// </summary>
        public static Type Authenticator
        {
            get => _authenticator;
            set
            {
                if (!value.IsSubclassDeep(typeof(AuthenticationProvider)))
                {
                    throw new ArgumentException($"{value.GetType()} is not a subclass of {typeof(AuthenticationProvider)}.");
                }
                _authenticator = value;
                return;
            }
        }

        private static Type _authenticator = null;

        /// <summary>
        /// The <see cref="INetworkObject"/> which will be spawned for new <see cref="NetworkClient"/>s who connect. By default, this object will have <see cref="INetworkObject.ObjectVisibilityMode"/> set to <see cref="ObjectVisibilityMode.Everyone"/> and <see cref="INetworkObject.OwnershipMode"/> set to <see cref="OwnershipMode.Client"/>.
        /// </summary>
        public static Type ClientAvatar = null;

        /// <summary>
        /// Should the server be currently accepting connections? if this is set to false, the server will not reply to socket requests.
        /// </summary>
        public bool ShouldAcceptConnections = true;

        /// <summary>
        /// Should clients be able to set themselves as ready using <see cref="SocketNetworking.Shared.PacketSystem.Packets.ReadyStateUpdatePacket"/>?
        /// </summary>
        public static bool AllowClientSelfReady = true;

        private static Thread _serverThread;

        protected static NetworkServer _serverInstance;

        /// <summary>
        /// Server singleton. Intended to prevent multiple servers running on the same process.
        /// </summary>
        public static NetworkServer ServerInstance
        {
            get
            {
                if (_serverInstance == null)
                {
                    Log.Error("Tried to get stopped server.");
                    throw new InvalidOperationException("Attempted to access server instance when it isn't running.");
                }
                else
                {
                    return _serverInstance;
                }
            }
            set
            {
                _serverInstance = value;
            }
        }

        protected readonly bool _isShuttingDown = false;

        private static readonly Dictionary<int, NetworkClient> _clients = new Dictionary<int, NetworkClient>();

        private static List<ClientHandler> handlers = new List<ClientHandler>();

        /// <summary>
        /// Starts the server. If it is already started (see <see cref="HasServerStarted"/>), does nothing. Runs <see cref="Validate"/>, if this fails, does nothing.
        /// </summary>
        public virtual void StartServer()
        {
            if (HasServerStarted)
            {
                Log.Error("Server already started!");
                return;
            }
            _serverState = ServerState.Started;
            if (!Validate())
            {
                Log.Error("Server validator error.");
                return;
            }
            _serverInstance = this;
            handlers.Capacity = Config.DefaultThreads;
            Log.Info($"Default Threads: {Config.DefaultThreads}");
            for (int i = 0; i < Config.DefaultThreads; i++)
            {
                ClientHandler handler = new ClientHandler();
                handlers.Add(handler);
                Log.Info("Start Thread ID: " + i);
                handler.Start();
            }
            _serverThread = new Thread(ServerStartThread);
            ClientConnecting += (sender, req) =>
            {
                if (Clients.Count < Config.MaximumClients)
                {
                    return;
                }
                //Do not accept.
                Log.Info("Rejected a client because the server is full.");
                req.Reject();
            };
            _serverThread.Start();
            ServerStarted?.Invoke();
            _serverState = ServerState.NotReady;
        }

        /// <summary>
        /// Validates the current server configuration. By default, checks <see cref="ClientType"/> and <see cref="ClientAvatar"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual bool Validate()
        {
            if (ClientType == null)
            {
                Log.Error("Can't start server: Client Type is not correct, cannot be null.");
                return false;
            }
            if (!ClientType.IsSubclassOf(typeof(NetworkClient)))
            {
                Log.Error("Can't start server: Client Type is not correct. Should be a subclass of NetworkClient");
                return false;
            }
            if (Config.MaximumClients % Config.DefaultThreads != 0)
            {
                Log.Warning("You have a mismatched client to thread ratio. Ensure that each thread can reserve the same amount of clients, meaning no remainder.");
            }
            if (ClientAvatar == null || ClientAvatar.GetInterfaces().Contains(typeof(INetworkAvatar)))
            {
                return true;
            }
            Log.Error("Server start error. Your client avatar must implement INetworkAvatar through either NetworkAvatarBase or a custom implementation.");
            return false;
        }

        /// <summary>
        /// Adds a <see cref="NetworkClient"/> to be handled by thread pools.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientId"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected static void AddClient(NetworkClient client, int clientId)
        {
            lock (ClientLock)
            {
                if (_clients.ContainsKey(clientId))
                {
                    Log.Error("Something really got fucked up!");
                    //we throw because the whole server will die if we cant add the client.
                    throw new InvalidOperationException("Client ID to add already taken!");
                }
                else
                {
                    _clients.Add(clientId, client);
                    ClientHandler handler = NextHandler();
                    handler.AddClient(client);
                    Log.Debug($"Handler Client count: {handler.CurrentClientCount}");
                    Log.Debug($"Added client. ID: {clientId}, Type: {client.GetType().FullName}");
                }
            }
        }

        private static ClientHandler NextHandler()
        {
            ClientHandler bestHandler = null;
            foreach (ClientHandler handler in handlers.Where(handler => handler.CurrentClientCount < Config.ClientsPerThread))
            {
                if (bestHandler == null)
                {
                    bestHandler = handler;
                    continue;
                }
                if (handler.CurrentClientCount < bestHandler.CurrentClientCount)
                {
                    bestHandler = handler;
                }
            }
            return bestHandler;
        }

        protected static readonly object ClientLock = new object();

        /// <summary>
        /// Removes a <see cref="NetworkClient"/> from the thread pools. If it is already removed, does nothing.
        /// </summary>
        /// <param name="myClient"></param>
        public static void RemoveClient(NetworkClient myClient)
        {
            lock (ClientLock)
            {
                if (_clients.ContainsKey(myClient.ClientID))
                {
                    ClientHandler handler = handlers.FirstOrDefault(x => x.HasClient(myClient));
                    if (handler == null)
                    {
                        Log.Error("Unable to find the handler responsible for Client ID " + myClient.ClientID);
                    }
                    else
                    {
                        handler.RemoveClient(_clients[myClient.ClientID]);
                    }
                    _clients.Remove(myClient.ClientID);
                    InvokeClientDisconnected(myClient);
                    NetworkManager.SendDisconnectedPulse(myClient);
                }
                else
                {
                    Log.Warning($"Can't remove client ID {myClient.ClientID}, not found!");
                    ClientHandler handler = handlers.FirstOrDefault(x => x.HasClient(myClient));
                    if (handler == null)
                    {
                        Log.Error("Unable to find the handler responsible for Client ID " + myClient.ClientID);
                    }
                    else
                    {
                        handler.RemoveClient(_clients[myClient.ClientID]);
                        InvokeClientDisconnected(myClient);
                        NetworkManager.SendDisconnectedPulse(myClient);
                    }
                }
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
            return _clients.TryGetValue(id, out NetworkClient client) ? client : null;
        }

        /// <summary>
        /// Stops the server. If the server has already stopped (see <see cref="HasServerStarted"/>), does nothing.
        /// </summary>
        public virtual void StopServer()
        {
            if (!HasServerStarted)
            {
                Log.Error("Server already stopped.");
                return;
            }
            ShouldAcceptConnections = false;
            List<NetworkClient> _clients = Clients;
            foreach (NetworkClient client in _clients)
            {
                client.Disconnect("Server shutting down");
            }
            _clients.Clear();
            _serverThread.Abort();
            ServerStopped?.Invoke();
        }

        protected virtual void ServerStartThread()
        {
            throw new NotImplementedException("Trying to use the non-overridden server thread, you should probably override it, do not run the base method!");
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to all connected clients.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="toReadyOnly"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void SendToAll(Packet packet)
        {
            SendToAll(packet, (x => true));
        }

        /// <summary>
        /// Sends a <see cref="TargetedPacket"/> to all clients. <paramref name="object"/> is used to set <see cref="TargetedPacket.NetworkIDTarget"/>.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="object"></param>
        public static void SendToAll(TargetedPacket packet, INetworkObject @object)
        {
            packet.NetworkIDTarget = @object.NetworkID;
            SendToAll(packet, x => @object.CheckVisibility(x));
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to all <see cref="NetworkClient"/> which match <paramref name="predicate"/>.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="predicate"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void SendToAll(Packet packet, Predicate<NetworkClient> predicate)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            lock (ClientLock)
            {
                foreach (NetworkClient client in _clients.Values.Where(client => predicate(client)))
                {
                    client.Send(packet);
                }
            }
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to all <see cref="NetworkClient"/> which match <paramref name="predicate"/>.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="predicate"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void SendToAll(TargetedPacket packet, INetworkObject @object, Predicate<NetworkClient> predicate)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            packet.NetworkIDTarget = @object.NetworkID;
            lock (ClientLock)
            {
                foreach (NetworkClient client in _clients.Values.Where(x => @object.CheckVisibility(x)).Where(client => predicate(client)))
                {
                    client.Send(packet);
                }
            }
        }

        public static void SentToAll(TargetedPacket packet, INetworkObject target, bool priority, bool toReadyOnly = false)
        {
            if (priority)
            {
                packet.Flags = priority ? packet.Flags |= PacketFlags.Priority : packet.Flags &= ~PacketFlags.Priority;
            }
            SendToAll(packet, target, (x) => !toReadyOnly || x.Ready);
        }

        /// <summary>
        /// Disconnects all clients who aren't ready.
        /// </summary>
        /// <param name="reason">
        /// The Disconnect reason.
        /// </param>
        public static void DisconnectNotReady(string reason = "Failed to ready in time.")
        {
            Disconnect(x => !x.Ready, reason);
        }

        /// <summary>
        /// Disconnects all <see cref="NetworkClient"/> which match <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void Disconnect(Predicate<NetworkClient> predicate)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            lock (ClientLock)
            {
                foreach (NetworkClient client in _clients.Values)
                {
                    client.Disconnect();
                }
            }
        }

        /// <summary>
        /// Disconnects all <see cref="NetworkClient"/>s which match <paramref name="predicate"/> with a <paramref name="reason"/>.
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="reason"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void Disconnect(Predicate<NetworkClient> predicate, string reason)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            lock (ClientLock)
            {
                foreach (NetworkClient client in _clients.Values)
                {
                    client.Disconnect(reason);
                }
            }
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to all ready clients
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> to send.
        /// </param>
        public static void SendToReady(Packet packet)
        {
            SendToAll(packet, x => x.Ready);
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
        public static void SendToReady(TargetedPacket packet, INetworkObject target)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            List<NetworkClient> readyClients = _clients.Values.Where(x => target.CheckVisibility(x)).Where(x => x.Ready).ToList();
            foreach (NetworkClient client in readyClients)
            {
                client.Send(packet, target);
            }
        }



        /// <summary>
        /// Runs <see cref="NetworkClient.NetworkInvoke(object, string, object[], bool)"/> on all clients. <paramref name="readyOnly"/> determines if the clients must be <see cref="NetworkClient.Ready"/> to be called on.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="readyOnly"></param>
        /// <param name="priority"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void NetworkInvokeOnAll(object obj, string methodName, object[] args, bool readyOnly = false)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            List<NetworkClient> clients = _clients.Values.ToList();
            if (obj is INetworkObject netObj)
            {
                clients = clients.Where(x => netObj.CheckVisibility(x)).ToList();
            }
            if (readyOnly)
            {
                clients = clients.Where(x => x.Ready).ToList();
            }
            foreach (NetworkClient client in clients)
            {
                client.NetworkInvoke(obj, methodName, args);
            }
        }


        /// <summary>
        /// Runs <see cref="NetworkClient.NetworkInvoke(object, string, object[])"/> on all clients. <paramref name="readyOnly"/> determines if the clients must be <see cref="NetworkClient.Ready"/> to be called on.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="filter"></param>
        /// <param name="priority"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void NetworkInvokeOnAll(object obj, string methodName, Predicate<NetworkClient> filter, params object[] args)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            List<NetworkClient> clients = _clients.Values.ToList();
            if (obj is INetworkObject netObj)
            {
                clients = clients.Where(x => netObj.CheckVisibility(x)).ToList();
            }
            clients = clients.Where(x => filter(x)).ToList();
            foreach (NetworkClient client in clients)
            {
                client.NetworkInvoke(obj, methodName, args);
            }
        }

        public static void NetworkInvokeOnAll(string methodName, object[] args, Predicate<NetworkClient> filter)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Server method called when server is not active!");
            }
            List<NetworkClient> clients = _clients.Values.ToList();
            clients = clients.Where(x => filter(x)).ToList();
            foreach (NetworkClient client in clients)
            {
                client.NetworkInvokeOnClient(methodName, args);
            }
        }

        public static void NetworkInvokeOnAll(string methodName, object[] args)
        {
            NetworkInvokeOnAll(methodName, args, x => true);
        }
    }


    /// <summary>
    /// Determines the server state.
    /// </summary>
    public enum ServerState
    {
        /// <summary>
        /// The server is not running at all.
        /// </summary>
        NotStarted,
        /// <summary>
        /// The server has started.
        /// </summary>
        Started,
        /// <summary>
        /// The server is not ready to accept connections, but is running.
        /// </summary>
        NotReady,
        /// <summary>
        /// The server is ready to accept connections.
        /// </summary>
        Ready,
    }

    /// <summary>
    /// <see cref="ServerEncryptionMode"/> determines the behavior of the encryption standard of the server - client connection. It is recommended you use <see cref="ServerEncryptionMode.Required"/>.
    /// </summary>
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
        Required,
    }
}
