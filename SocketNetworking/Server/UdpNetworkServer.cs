﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Events;
using SocketNetworking.Shared.Transports;

namespace SocketNetworking.Server
{
    public class UdpNetworkServer : NetworkServer
    {
        protected Dictionary<IPEndPoint, UdpNetworkClient> _udpClients = new Dictionary<IPEndPoint, UdpNetworkClient>();

        public static IPEndPoint MyEndPoint
        {
            get
            {
                return new IPEndPoint(Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(), Config.Port);
            }
        }

        protected override void ServerStartThread()
        {
            Log.GlobalInfo("Server starting...");
            Log.GlobalInfo($"Listening on {Config.BindIP}:{Config.Port}");
            int counter = 0;
            IPEndPoint listener = new IPEndPoint(IPAddress.Any, Config.Port);
            UdpClient udpClient = new UdpClient(listener);
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
                if (Clients.Count >= Config.MaximumClients)
                {
                    continue;
                }
                byte[] Receive = udpClient.Receive(ref listener);
                IPEndPoint remoteIpEndPoint = listener;
                if (!_udpClients.ContainsKey(remoteIpEndPoint))
                {
                    Log.GlobalInfo($"Connecting client {counter} from {remoteIpEndPoint.Address}:{remoteIpEndPoint.Port}");
                    NetworkClient client = (NetworkClient)Activator.CreateInstance(ClientType);
                    UdpTransport transport = new UdpTransport();
                    transport.Client = udpClient;
                    transport.SetupForServerUse(remoteIpEndPoint, MyEndPoint);
                    client.InitRemoteClient(counter, transport);
                    AddClient(client, counter);
                    _udpClients.Add(remoteIpEndPoint, client as UdpNetworkClient);
                    transport.ServerReceive(Receive, remoteIpEndPoint);
                    ClientConnectRequest disconnect = AcceptClient(client);
                    if (!disconnect.Accepted)
                    {
                        client.Disconnect(disconnect.Message);
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
                    counter++;
                }
                else
                {
                    UdpNetworkClient client = _udpClients[remoteIpEndPoint];
                    UdpTransport transport = client.UdpTransport;
                    transport.ServerReceive(Receive, remoteIpEndPoint);
                }
            }
            Log.GlobalInfo("Shutting down!");
            return;
        }
    }
}
