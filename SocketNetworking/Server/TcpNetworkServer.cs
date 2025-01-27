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

namespace SocketNetworking.Server
{
    public class TcpNetworkServer : NetworkServer
    {
        protected override void ServerStartThread()
        {
            Log.GlobalInfo("Server starting...");
            TcpListener serverSocket = new TcpListener(IPAddress.Parse(Config.BindIP), Config.Port);
            serverSocket.Start();
            Log.GlobalInfo("Socket Started.");
            Log.GlobalInfo($"Listening on {Config.BindIP}:{Config.Port}");
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
                TcpTransport tcpTransport = new TcpTransport();
                tcpTransport.Client = socket;
                socket.NoDelay = true;
                IPEndPoint remoteIpEndPoint = socket.Client.RemoteEndPoint as IPEndPoint;
                Log.GlobalInfo($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
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
            Log.GlobalInfo("Shutting down!");
            serverSocket.Stop();
        }
    }
}
