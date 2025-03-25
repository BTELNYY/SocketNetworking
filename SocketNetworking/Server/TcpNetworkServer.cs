using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Events;
using SocketNetworking.Shared.Transports;

namespace SocketNetworking.Server
{
    public class TcpNetworkServer : NetworkServer
    {
        public override void StartServer()
        {
            if (Config.CertificatePath == "")
            {
                Log.Info("No SSL Certificate found, ignoring.");
            }
            else
            {
                Log.Info($"Found an SSL Certificate: {Config.CertificatePath}, Checking.");
                if (!File.Exists(Config.CertificatePath))
                {
                    Log.Warning($"Certificate couldn't be loaded: '{Config.CertificatePath}' is not found.");
                }
                else
                {
                    X509Certificate cert = X509Certificate.CreateFromCertFile(Config.CertificatePath);
                    if (cert == null)
                    {
                        Log.Warning("Certificate couldn't be loaded.");
                    }
                    else
                    {
                        Config.Certificate = cert;
                    }
                }
            }
            base.StartServer();
        }

        protected override void ServerStartThread()
        {
            Log.Info("Server starting...");
            TcpListener serverSocket = new TcpListener(IPAddress.Parse(Config.BindIP), Config.Port);
            serverSocket.Start();
            Log.Info("Socket Started.");
            Log.Info($"Listening on {Config.BindIP}:{Config.Port}");
            int counter = 0;
            _serverState = ServerState.Ready;
            InvokeServerReady();
            while (true)
            {
                if (_isShuttingDown)
                {
                    break;
                }
                if (!serverSocket.Pending())
                {
                    continue;
                }
                if (!ShouldAcceptConnections)
                {
                    continue;
                }
                TcpClient socket = serverSocket.AcceptTcpClient();
                _ = Task.Run(() =>
                {
                    TcpTransport tcpTransport = new TcpTransport(socket);
                    tcpTransport.Socket.NoDelay = true;
                    IPEndPoint remoteIpEndPoint = socket.Client.RemoteEndPoint as IPEndPoint;
                    Log.Info($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
                    TcpNetworkClient client = (TcpNetworkClient)Activator.CreateInstance(ClientType);
                    client.InitRemoteClient(counter, tcpTransport);
                    AddClient(client, counter);
                    ClientConnectRequest disconnect = AcceptClient(client);
                    if (!disconnect.Accepted)
                    {
                        client.Disconnect(disconnect.Message);
                        socket?.Close();
                        return;
                    }
                    CallbackTimer<NetworkClient> callback = new CallbackTimer<NetworkClient>((x) =>
                    {
                        if (x == null)
                        {
                            return;
                        }
                        if (x.CurrentConnectionState != ConnectionState.Connected)
                        {
                            x.Disconnect("Failed to handshake in time.");
                        }
                    }, client, Config.HandshakeTime);
                    callback.Start();
                    InvokeClientConnected(client);
                });
                counter++;
            }
            Log.Info("Shutting down!");
            serverSocket.Stop();
        }
    }
}
