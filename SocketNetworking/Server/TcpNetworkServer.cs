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

namespace SocketNetworking.Server
{
    public class TcpNetworkServer : NetworkServer
    {
        public override void StartServer()
        {
            if (Config.SSLCertificate == "")
            {
                Log.Info("No SSL Certificate found, ignoring.");
            }
            else
            {
                Log.Info($"Found an SSL Certificate: {Config.SSLCertificate}, Checking.");
                var cert = X509Certificate.CreateFromCertFile(Config.SSLCertificate);
                if (cert == null)
                {
                    Log.Warning("Certificate couldn't be loaded.");
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
            InvokeServerReady();
            _serverState = ServerState.Ready;
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
                TcpTransport tcpTransport = new TcpTransport(socket);
                socket.NoDelay = true;
                IPEndPoint remoteIpEndPoint = socket.Client.RemoteEndPoint as IPEndPoint;
                Log.Info($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
                NetworkClient client = (NetworkClient)Activator.CreateInstance(ClientType);
                client.InitRemoteClient(counter, tcpTransport);
                AddClient(client, counter);
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
                InvokeClientConnected(counter);
                counter++;
            }
            Log.Info("Shutting down!");
            serverSocket.Stop();
        }
    }
}
