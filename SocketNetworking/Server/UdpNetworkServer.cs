using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Transports;
using SocketNetworking.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Server
{
    public class UdpNetworkServer : NetworkServer
    {
        protected Dictionary<IPEndPoint, UdpNetworkClient> _udpClients = new Dictionary<IPEndPoint, UdpNetworkClient> ();

        protected override NetworkServer GetServer()
        {
            return new UdpNetworkServer();
        }

        protected override void ServerStartThread()
        {
            Log.GlobalInfo("Server starting...");
            IPEndPoint listener = new IPEndPoint(IPAddress.Any, Port);
            UdpClient udpClient = new UdpClient(listener);
            Log.GlobalInfo($"Listening on {BindIP}:{Port}");
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
                byte[] recieve = udpClient.Receive(ref listener);
                IPEndPoint remoteIpEndPoint = listener as IPEndPoint;
                if (!_udpClients.ContainsKey(remoteIpEndPoint))
                {
                    Log.GlobalInfo($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
                    NetworkClient client = (NetworkClient)Activator.CreateInstance(ClientType);
                    UdpTransport transport = new UdpTransport();
                    transport.SetupForServerUse(remoteIpEndPoint, (IPEndPoint)udpClient.Client.LocalEndPoint);
                    client.InitRemoteClient(counter, transport);
                    AddClient(client, counter);
                    _udpClients.Add(remoteIpEndPoint, client as UdpNetworkClient);
                    transport.ServerRecieve(recieve);
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
                    }, client, HandshakeTime);
                    callback.Start();
                    InvokeClientConnected(counter);
                    counter++;
                }
                else
                {
                    UdpNetworkClient client = _udpClients[remoteIpEndPoint];
                    UdpTransport transport = client.UdpTransport;
                    transport.ServerRecieve(recieve);
                }
            }
            Log.GlobalInfo("Shutting down!");
            return;
        }
    }
}
