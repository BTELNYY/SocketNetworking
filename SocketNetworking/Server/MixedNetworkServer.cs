using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;
using System.Reflection;
using SocketNetworking.PacketSystem;
using System.Threading;

namespace SocketNetworking.Server
{
    public class MixedNetworkServer : NetworkServer
    {
        protected List<MixedNetworkClient> _awaitingUDPConnection = new List<MixedNetworkClient>();

        protected Dictionary<IPEndPoint, MixedNetworkClient> _udpClients = new Dictionary<IPEndPoint, MixedNetworkClient>();

        protected Thread UdpReader;

        public static IPEndPoint MyEndPoint
        {
            get
            {
                return new IPEndPoint(Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(), Port);
            }
        }

        protected override NetworkServer GetServer()
        {
            return new MixedNetworkServer();
        }

        protected override void ServerStartThread()
        {
            Log.GlobalInfo("Server starting...");
            UdpReader = new Thread(AcceptUDP);
            UdpReader.Start();
            TcpListener serverSocket = new TcpListener(IPAddress.Parse(BindIP), Port);
            serverSocket.Start();
            Log.GlobalInfo("Mixed Client Started.");
            Log.GlobalInfo($"Listening on {BindIP}:{Port} (UDP/TCP)");
            int counter = 0;
            InvokeServerReady();
            _serverState = ServerState.Ready;
            while (true)
            {
                if (_isShuttingDown)
                {
                    break;
                }
                if (!ShouldAcceptConnections)
                {
                    continue;
                }
                if (!serverSocket.Pending())
                {
                    continue;
                }
                TcpClient socket = serverSocket.AcceptTcpClient();
                TcpTransport tcpTransport = new TcpTransport();
                tcpTransport.Client = socket;
                socket.NoDelay = true;
                IPEndPoint remoteIpEndPoint = socket.Client.RemoteEndPoint as IPEndPoint;
                Log.GlobalInfo($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
                MixedNetworkClient client = (MixedNetworkClient)Activator.CreateInstance(ClientType);
                client.InitRemoteClient(counter, tcpTransport);
                AddClient(client, counter);
                CallbackTimer<NetworkClient> callback = new CallbackTimer<NetworkClient>((x) =>
                {
                    if (x == null)
                    {
                        return;
                    }
                    if(_awaitingUDPConnection.Contains(x))
                    {
                        x.Disconnect("Failed to Establish UDP in time.");
                    }
                    if (x.CurrentConnectionState != ConnectionState.Connected)
                    {
                        x.Disconnect("Failed to handshake in time.");
                    }
                }, client, HandshakeTime);
                callback.Start();
                InvokeClientConnected(counter);
                counter++;
            }
            Log.GlobalInfo("Shutting down!");
            serverSocket.Stop();
        }

        void AcceptUDP()
        {
            Log.GlobalInfo("UDP Server starting...");
            Log.GlobalInfo($"Listening on {BindIP}:{Port}");
            IPEndPoint listener = new IPEndPoint(IPAddress.Any, Port);
            UdpClient udpClient = new UdpClient(listener);
            _serverState = ServerState.Ready;
            while (true)
            {
                if (_isShuttingDown)
                {
                    break;
                }
                if (!ShouldAcceptConnections)
                {
                    continue;
                }
                byte[] recieve = udpClient.Receive(ref listener);
                ByteReader reader = new ByteReader(recieve);
                int netId = reader.ReadInt();
                int passKey = reader.ReadInt();
                IPEndPoint remoteIpEndPoint = listener as IPEndPoint;
                if (!_udpClients.ContainsKey(remoteIpEndPoint))
                {
                    Log.GlobalInfo($"Connecting client {netId} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
                    NetworkClient client = _awaitingUDPConnection.Find(x => x.InitialUDPKey == passKey && x.ClientID == netId);
                    if(client == default(NetworkClient))
                    {
                        Log.GlobalError($"There was an error finding the client with NetID: {netId} and Passkey: {passKey}");
                        continue;
                    }
                    UdpTransport transport = new UdpTransport();
                    transport.Client = udpClient;
                    transport.SetupForServerUse(remoteIpEndPoint, MyEndPoint);
                    _udpClients.Add(remoteIpEndPoint, client as MixedNetworkClient);
                    transport.ServerRecieve(recieve);
                }
                else
                {
                    MixedNetworkClient client = _udpClients[remoteIpEndPoint];
                    UdpTransport transport = client.UdpTransport;
                    transport.ServerRecieve(recieve);
                }
            }
            Log.GlobalInfo("Shutting down UDP Server!");
            return;
        }
    }
}
