#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Events;

namespace SocketNetworking.Server
{
    [RequiresPreviewFeatures]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macOS")]
    public class QuicNetworkServer : NetworkServer
    {
        public override void StartServer()
        {
            base.StartServer();
        }

        private QuicListener _listener;

        /// <summary>
        /// Server authentication options to be used.
        /// </summary>
        public SslServerAuthenticationOptions ServerAuthenticationOptions { get; set; }

        /// <summary>
        /// QUIC Server connection options
        /// </summary>
        public QuicServerConnectionOptions ConnectionOptions { get; set; } = new QuicServerConnectionOptions();

        protected override bool Validate()
        {
            if (Config.Certificate == null && Config.CertificatePath == "")
            {
                Log.Error("QUIC Requires a certificate.");
                return false;
            }
            else if (Config.CertificatePath != "")
            {
                X509Certificate cert = new X509Certificate(Config.CertificatePath, Config.CertificatePassword);
                Config.Certificate = cert;
                Log.Info("Loaded default certificate for: " + cert.Subject);
            }
            return base.Validate();
        }

        protected override void ServerStartThread()
        {
            Log.Info("Starting QUIC Server...");
            if (Config.Certificate == null)
            {
                throw new InvalidOperationException("QUIC Requires a certificate.");
            }
            ServerAuthenticationOptions = new SslServerAuthenticationOptions
            {
                ApplicationProtocols = [new SslApplicationProtocol(ServerConfiguration.Protocol)],
                ServerCertificate = Config.Certificate,
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                {
                    return true;
                },
                ServerCertificateSelectionCallback = (sender, host) =>
                {
                    if (Config.Certificate == null)
                    {
                        Log.Error("Certificate null!");
                    }
                    return Config.Certificate;
                },
                ClientCertificateRequired = false,
            };
            ConnectionOptions = new QuicServerConnectionOptions()
            {
                DefaultCloseErrorCode = QuicNetworkClient.DefaultErrorCode,
                DefaultStreamErrorCode = QuicNetworkClient.DefaultStreamClosedCode,
                ServerAuthenticationOptions = this.ServerAuthenticationOptions
            };
            Task.Run(async () =>
            {
                _listener = await QuicListener.ListenAsync(new QuicListenerOptions()
                {
                    ListenEndPoint = IPEndPoint.Parse($"{Config.BindIP}:{Config.Port}"),
                    ApplicationProtocols = new List<System.Net.Security.SslApplicationProtocol>() { new SslApplicationProtocol(ServerConfiguration.Protocol) },
                    ConnectionOptionsCallback = async (con, info, token) =>
                    {
                        return ConnectionOptions;
                    }
                });
                Log.Info($"Started listening on {_listener.LocalEndPoint.Address}:{_listener.LocalEndPoint.Port}");
                int counter = 0;
                while (!_isShuttingDown)
                {
                    try
                    {
                        QuicConnection connection = await _listener.AcceptConnectionAsync();
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                Log.Info($"Connecting client {counter} from {connection.RemoteEndPoint.Address}:{connection.RemoteEndPoint.Port}");
                                QuicNetworkClient client = (QuicNetworkClient)Activator.CreateInstance(ClientType);
                                QuicStream stream = await connection.AcceptInboundStreamAsync();
                                client.QuicTransport.Connection = connection;
                                client.QuicTransport.Stream = stream;
                                client.InitRemoteClient(counter, client.QuicTransport);
                                AddClient(client, counter);
                                ClientConnectRequest disconnect = AcceptClient(client);
                                if (!disconnect.Accepted)
                                {
                                    await client.DisconnectAsync(disconnect.Message);
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
                                InvokeClientConnected(client);
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Failed accepting client. Error: " + ex.ToString());
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }).Wait();
        }
    }
}
#endif