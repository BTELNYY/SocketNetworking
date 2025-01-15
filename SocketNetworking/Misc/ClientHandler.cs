using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Server;

namespace SocketNetworking.Misc
{
    public class ClientHandler : IRoundRobinData<ClientHandler>
    {
        public Thread Thread { get; }

        /// <summary>
        /// Not thread safe.
        /// </summary>
        public HashSet<NetworkClient> Clients { get; }

        public ClientHandler(IEnumerable<NetworkClient> clients)
        {
            Thread = new Thread(Run);
            Clients = new HashSet<NetworkClient>(clients);
        }

        public int CurrentClientCount
        {
            get
            {
                return Clients.Count;
            }
        }

        public bool AllowChoosing => CurrentClientCount < NetworkServer.Config.ClientsPerThread;

        public bool AllowSorting => true;

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
                    foreach (NetworkClient client in Clients)
                    {
                        client.ReadNext();
                        client.WriteNext();
                    }
                }
            }
        }

        public int CompareTo(ClientHandler other)
        {
            return CurrentClientCount - other.CurrentClientCount;
        }
    }
}
