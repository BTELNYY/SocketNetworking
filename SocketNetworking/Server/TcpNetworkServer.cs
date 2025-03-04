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
using System.Security.Cryptography.X509Certificates;
using System.IO;

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
                if(!File.Exists(Config.CertificatePath))
                {
                    Log.Warning($"Certificate couldn't be loaded: '{Config.CertificatePath}' is not found.");
                }
                else
                {
                    var cert = X509Certificate.CreateFromCertFile(Config.CertificatePath);
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
                    IPEndPoint remoteIpEndPoint = socket.Client.RemoteEndPoint as IPEndPoint;
                    Log.Info($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
                    TcpNetworkClient client = (TcpNetworkClient)Activator.CreateInstance(ClientType);
                    client.InitRemoteClient(counter, tcpTransport);
                    client.TcpNoDelay = true;
                    AddClient(client, counter);
                    bool disconnect = !AcceptClient(client);
                    if (disconnect)
                    {
                        client.Disconnect();
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
