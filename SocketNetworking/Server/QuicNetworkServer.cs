#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Events;

namespace SocketNetworking.Server
{
    [RequiresPreviewFeatures]
    public class QuicNetworkServer : NetworkServer
    {
        public override void StartServer()
        {
            base.StartServer();
        }

        private QuicListener _listner;

        protected override void ServerStartThread()
        {
            Log.Info("Starting QUIC Server...");
            Task<QuicListener> listener = QuicListener.ListenAsync(new QuicListenerOptions()
            {
                ListenEndPoint = IPEndPoint.Parse($"{Config.BindIP}:{Config.Port}"),
                ApplicationProtocols = new List<System.Net.Security.SslApplicationProtocol>() { System.Net.Security.SslApplicationProtocol.Http2 }
            }).AsTask();
            listener.Wait();
            Log.Info($"Started listening on {Config.BindIP}:{Config.Port}");
            _listner = listener.Result;
            int counter = 0;
            while (!_isShuttingDown)
            {
                _ = Task.Run(async () =>
                {
                    QuicConnection connection = await _listner.AcceptConnectionAsync();
                    QuicNetworkClient client = new QuicNetworkClient();
                    client.InitRemoteClient(counter, null);
                    client.SetupRemoteClient(connection);
                    AddClient(client, counter);
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
                    counter++;
                });
            }
        }
    }
}

#endif