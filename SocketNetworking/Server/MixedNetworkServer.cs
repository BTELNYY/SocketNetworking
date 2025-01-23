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
using System.IO.Ports;

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
                return new IPEndPoint(Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(), Config.Port);
            }
        }

        protected override NetworkServer GetServer()
        {
            return new MixedNetworkServer();
        }

        protected override void ServerStartThread()
        {
            Log.GlobalInfo("Server starting...");
            TcpListener serverSocket = new TcpListener(IPAddress.Parse(Config.BindIP), Config.Port);
            serverSocket.Start();
            Log.GlobalInfo("Mixed Client Started.");
            Log.GlobalInfo($"Listening on {Config.BindIP}:{Config.Port} (TCP)");
            UdpReader = new Thread(AcceptUDP);
            UdpReader.Start();
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
                if (Clients.Count >= Config.MaximumClients)
                {
                    //Do not accept.
                    Log.GlobalInfo("Rejected a client becuase the server is full.");
                    socket.Close();
                    continue;
                }
                TcpTransport tcpTransport = new TcpTransport();
                tcpTransport.Client = socket;
                socket.NoDelay = true;
                IPEndPoint remoteIpEndPoint = socket.Client.RemoteEndPoint as IPEndPoint;
                Log.GlobalInfo($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port} on TCP.");
                MixedNetworkClient client = (MixedNetworkClient)Activator.CreateInstance(ClientType);
                client.InitRemoteClient(counter, tcpTransport);
                AddClient(client, counter);
                _awaitingUDPConnection.Add(client);
                CallbackTimer<MixedNetworkClient> callback = new CallbackTimer<MixedNetworkClient>((x) =>
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
                }, client, Config.HandshakeTime);
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
            Log.GlobalInfo($"Listening on {Config.BindIP}:{Config.Port} (UDP)");
            IPEndPoint listener = new IPEndPoint(IPAddress.Any, Config.Port);
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
                    Log.GlobalInfo($"Connecting client {netId} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port} on UDP.");
                    MixedNetworkClient client = _awaitingUDPConnection.Find(x => x.InitialUDPKey == passKey && x.ClientID == netId);
                    if(client == default(NetworkClient))
                    {
                        Log.GlobalError($"There was an error finding the client with NetID: {netId} and Passkey: {passKey}");
                        continue;
                    }
                    UdpTransport transport = client.UdpTransport;
                    transport.Client = udpClient;
                    transport.SetupForServerUse(remoteIpEndPoint, MyEndPoint);
                    _udpClients.Add(remoteIpEndPoint, client as MixedNetworkClient);
                    //Dont read the first message since its not actually a packet, and just the client ID and the passkey.
                    //transport.ServerRecieve(recieve);
                    _awaitingUDPConnection.Remove((MixedNetworkClient)client);
                }
                else
                {
                    MixedNetworkClient client = _udpClients[remoteIpEndPoint];
                    client.UdpTransport.ServerRecieve(recieve);
                }
            }
            Log.GlobalInfo("Shutting down UDP Server!");
            return;
        }
    }
}
