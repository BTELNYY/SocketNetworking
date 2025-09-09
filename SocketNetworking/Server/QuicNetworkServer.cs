#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
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

        public SslServerAuthenticationOptions ServerAuthenticationOptions { get; private set; }

        protected override void ServerStartThread()
        {
            Log.Info("Starting QUIC Server...");
            QuicServerConnectionOptions connectionOptions = new QuicServerConnectionOptions()
            {
                DefaultCloseErrorCode = QuicNetworkClient.DefaultErrorCode,

                DefaultStreamErrorCode = QuicNetworkClient.DefaultStreamClosedCode,

                ServerAuthenticationOptions = new SslServerAuthenticationOptions
                {
                    // Specify the application protocols that the server supports. This list must be a subset of the protocols specified in QuicListenerOptions.ApplicationProtocols.
                    ApplicationProtocols = [new SslApplicationProtocol(ServerConfiguration.Protocol)],
                    // Server certificate, it can also be provided via ServerCertificateContext or ServerCertificateSelectionCallback.
                    ServerCertificate = Config.Certificate,
                }
            };
            Task<QuicListener> listener = QuicListener.ListenAsync(new QuicListenerOptions()
            {
                ListenEndPoint = IPEndPoint.Parse($"{Config.BindIP}:{Config.Port}"),
                ApplicationProtocols = new List<System.Net.Security.SslApplicationProtocol>() { System.Net.Security.SslApplicationProtocol.Http2 },
                ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(connectionOptions)
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
                    Log.Info($"Connecting client {counter} from {connection.RemoteEndPoint.Address}:{connection.RemoteEndPoint.Port}");
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