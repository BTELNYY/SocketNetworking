using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.Client;

namespace SocketNetworking.Misc
{
    public class ClientHandler
    {
        [Obsolete("Always is null as threading is no longer used.")]
        public Thread Thread { get; }

        public List<NetworkClient> Clients
        {
            get
            {
                lock (_lock)
                {
                    return _clients;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _clients = value;
                }
            }
        }

        private List<NetworkClient> _clients;

        private CancellationToken _token;

        private CancellationTokenSource _tokenSource;

        private Log _log;

        public Log Log => _log;

        public ClientHandler(IEnumerable<NetworkClient> clients)
        {
            _log = new Log("[ClientHandler]");
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            _clients = new List<NetworkClient>();
            foreach (NetworkClient client in clients)
            {
                AddClient(client);
            }
            //_clients = new List<NetworkClient>(clients);
        }

        public int CurrentClientCount
        {
            get
            {
                lock (_lock)
                {
                    return Clients.Count;
                }
            }
        }

        public void AddClient(NetworkClient client)
        {
            lock (_lock)
            {
                Clients.Add(client);
            }
            Task.Run(async () => await HandleClient(client), _token);
        }

        private async Task HandleClient(NetworkClient client)
        {
            while (!die)
            {
                if (client.CurrentConnectionState == Shared.ConnectionState.Disconnected)
                {
                    RemoveClient(client);
                    Log.Info($"Client {client.ClientID} removed, client is disconnected.");
                    break;
                }
                await client.ReadNextAsync();
                await client.WriteNextAsync();
            }
        }

        public void RemoveClient(NetworkClient client)
        {
            lock (_lock)
            {
                Clients.Remove(client);
            }
        }

        public bool HasClient(NetworkClient client)
        {
            lock (_lock)
            {
                return Clients.Contains(client);
            }
        }

        public void ClearClients()
        {
            lock (_lock)
            {
                Clients.Clear();
            }
        }

        public ClientHandler() : this(new List<NetworkClient>()) { }

        bool die = false;

        [Obsolete("Does nothing as clients will now use a Task system, the task is started when a client is added.")]
        public void Start()
        {
            //Thread.Start();
        }

        public void Stop()
        {
            _tokenSource.Cancel();
            die = true;
        }

        object _lock = new object();

        [Obsolete("Use Async tasks")]
        void Run()
        {
            while (true)
            {
                if (die) return;
                lock (_lock)
                {
                    for (int i = 0; i < _clients.Count; i++)
                    {
                        if (i >= _clients.Count)
                        {
                            break;
                        }
                        NetworkClient client = _clients[i];
                        if (client.CurrentConnectionState == Shared.ConnectionState.Disconnected || client.ShuttingDown)
                        {
                            Log.GlobalWarning($"Client {i} is being removed from the current handler, it has been disconnected.");
                            _clients.RemoveAt(i);
                        }
                        else
                        {
                            //client.Log.Debug("RUNNER: READ");
                            client.ReadNext();
                            //client.Log.Debug("RUNNER: WRITE");
                            client.WriteNext();
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"Clients: {CurrentClientCount}";
        }
    }
}
