#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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

        private QuicListener _listner;

        public SslServerAuthenticationOptions ServerAuthenticationOptions { get; private set; }

        protected override bool Validate()
        {
            if (Config.Certificate == null && Config.CertificatePath == "")
            {
                Log.Error("QUIC Requires a certificate.");
                return false;
            }
            else if (Config.CertificatePath != "")
            {
                X509Certificate cert = new X509Certificate(Config.CertificatePath);
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
            QuicServerConnectionOptions connectionOptions = new QuicServerConnectionOptions()
            {
                DefaultCloseErrorCode = QuicNetworkClient.DefaultErrorCode,
                DefaultStreamErrorCode = QuicNetworkClient.DefaultStreamClosedCode,
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
                }
            };
            Task<QuicListener> listener = QuicListener.ListenAsync(new QuicListenerOptions()
            {
                ListenEndPoint = IPEndPoint.Parse($"{Config.BindIP}:{Config.Port}"),
                ApplicationProtocols = new List<System.Net.Security.SslApplicationProtocol>() { new SslApplicationProtocol(ServerConfiguration.Protocol) },
                ConnectionOptionsCallback = async (con, info, token) => 
                {   
                    return connectionOptions;
                }
            }).AsTask();
            listener.Wait();
            _listner = listener.Result;
            Log.Info($"Started listening on {_listner.LocalEndPoint.Address}:{_listner.LocalEndPoint.Port}");
            int counter = 0;
            while (!_isShuttingDown)
            {
                try
                {
                    _listner.AcceptConnectionAsync().AsTask().Wait();
                    QuicConnection connection = _listner.AcceptConnectionAsync().AsTask().Result;
                    _ = Task.Run(async () =>
                    {
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
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            };
            
        }
        
    }
    
}


#endif