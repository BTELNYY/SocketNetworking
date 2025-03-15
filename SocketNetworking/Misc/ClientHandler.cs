using SocketNetworking.Client;
using System.Collections.Generic;
using System.Threading;

namespace SocketNetworking.Misc
{
    public class ClientHandler
    {
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

        public ClientHandler(IEnumerable<NetworkClient> clients)
        {
            Thread = new Thread(Run);
            _clients = new List<NetworkClient>(clients);
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

        public ClientHandler() : this(new List<NetworkClient>()) { }

        bool die = false;

        public void Start()
        {
            Thread.Start();
        }

        public void Stop()
        {
            die = true;
            Thread.Abort();
        }

        object _lock = new object();

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
                            _clients.RemoveAt(i);
                        }
                        else
                        {
                            client.ReadNext();
                            client.WriteNext();
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"Clients: {CurrentClientCount}, Thread: {Thread}";
        }
    }
}
