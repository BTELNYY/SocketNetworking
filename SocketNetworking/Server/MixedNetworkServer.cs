using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Transports;

namespace SocketNetworking.Server
{
    public class MixedNetworkServer : TcpNetworkServer
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

        protected override void ServerStartThread()
        {
            ClientDisconnected += (dcClient) =>
            {
                lock(clientLock)
                {
                    var client = _udpClients.FirstOrDefault(x => x.Value == dcClient).Key;
                    if(client == default)
                    {
                        return;
                    }
                    _udpClients.Remove(client);
                }
            };
            Log.Info("Server starting...");
            TcpListener serverSocket = new TcpListener(IPAddress.Parse(Config.BindIP), Config.Port);
            serverSocket.Start();
            Log.Info("Mixed Server Started.");
            Log.Info($"Listening on {Config.BindIP}:{Config.Port} (TCP)");
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
                    Log.Info("Rejected a client becuase the server is full.");
                    socket.Close();
                    continue;
                }
                _ = Task.Run(() => 
                {
                    TcpTransport tcpTransport = new TcpTransport(socket);
                    socket.NoDelay = true;
                    IPEndPoint remoteIpEndPoint = socket.Client.RemoteEndPoint as IPEndPoint;
                    Log.Info($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port} on TCP.");
                    MixedNetworkClient client = (MixedNetworkClient)Activator.CreateInstance(ClientType);
                    client.InitRemoteClient(counter, tcpTransport);
                    AddClient(client, counter);
                    _awaitingUDPConnection.Add(client);
                    InvokeClientConnected(client);
                    bool disconnect = !AcceptClient(client);
                    if (disconnect)
                    {
                        client.Disconnect();
                        socket?.Close();
                        return;
                    }
                    CallbackTimer<MixedNetworkClient> callback = new CallbackTimer<MixedNetworkClient>((x) =>
                    {
                        if (x == null)
                        {
                            return;
                        }
                        if (_awaitingUDPConnection.Contains(x))
                        {
                            x.Disconnect("Failed to Establish UDP in time.");
                        }
                        if (x.CurrentConnectionState != ConnectionState.Connected)
                        {
                            x.Disconnect("Failed to handshake in time.");
                        }
                    }, client, Config.HandshakeTime, x => true, (x) => 
                    {
                        if (x == null)
                        {
                            return false;
                        }
                        if(!_awaitingUDPConnection.Contains(x))
                        {
                            return false;
                        }
                        return true;
                    });
                    callback.Start();
                });
                counter++;
            }
            Log.Info("Shutting down!");
            serverSocket.Stop();
        }

        void AcceptUDP()
        {
            Log.Info("UDP Server starting...");
            Log.Info($"Listening on {Config.BindIP}:{Config.Port} (UDP)");
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
                try
                {
                    byte[] recieve = udpClient.Receive(ref listener);
                    IPEndPoint remoteIpEndPoint = listener as IPEndPoint;
                    if (!_udpClients.ContainsKey(remoteIpEndPoint))
                    {
                        ByteReader reader = new ByteReader(recieve);
                        int netId = reader.ReadInt();
                        int passKey = reader.ReadInt();
                        Log.Info($"Connecting client {netId} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port} on UDP.");
                        MixedNetworkClient client = _awaitingUDPConnection.Find(x => x.InitialUDPKey == passKey && x.ClientID == netId);
                        if (client == default(NetworkClient))
                        {
                            Log.Error($"There was an error finding the client with NetID: {netId} and Passkey: {passKey}");
                            continue;
                        }
                        client.UdpTransport = new UdpTransport();
                        client.UdpTransport.Client = udpClient;
                        client.UdpTransport.SetupForServerUse(remoteIpEndPoint, MyEndPoint);
                        lock(clientLock)
                        {
                            _udpClients.Add(remoteIpEndPoint, client);
                        }
                        //Dont read the first message since its not actually a packet, and just the client ID and the passkey.
                        _awaitingUDPConnection.Remove((MixedNetworkClient)client);
                    }
                    else
                    {
                        MixedNetworkClient client = _udpClients[remoteIpEndPoint];
                        client.UDPFailures = 0;
                        client.UdpTransport.ServerRecieve(recieve, remoteIpEndPoint);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error("UDP Client Listener Error: \n" + ex.ToString());
                    if(_udpClients.ContainsKey(listener))
                    {
                        MixedNetworkClient client = _udpClients[listener];
                        if(client.UDPFailures > 5)
                        {
                            if(!client.IsConnected || !client.UdpTransport.IsConnected)
                            {
                                lock(clientLock)
                                {
                                    _udpClients.Remove(listener);
                                }
                            }
                            client.Disconnect("UDP errors reached limit, You have failed to transmit valid packets.");
                            continue;
                        }
                        client.UDPFailures++;
                    }
                    else
                    {
                        Log.Debug(listener.ToString());
                    }
                    continue;
                }
            }
            Log.Info("Shutting down UDP Server!");
            udpClient.Dispose();
            return;
        }
    }
}
