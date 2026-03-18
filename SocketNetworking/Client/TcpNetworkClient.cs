using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Transports;

namespace SocketNetworking.Client
{
    /// <summary>
    /// The <see cref="TcpNetworkClient"/> class uses the <see cref="Shared.Transports.TcpTransport"/> to send data.
    /// </summary>
    public class TcpNetworkClient : NetworkClient
    {
        public TcpNetworkClient() : base()
        {
            Transport = new TcpTransport();
        }

        public bool SupportsSSL => true;

        public override NetworkTransport Transport
        {
            get
            {
                return base.Transport;
            }
            set
            {
                if (value is TcpTransport tcp)
                {
                    base.Transport = tcp;
                }
                else
                {
                    throw new InvalidOperationException("TcpNetworkClient does not support non-tcp transport.");
                }
            }
        }

        public TcpTransport TcpTransport
        {
            get
            {
                return (TcpTransport)Transport;
            }
            set
            {
                Transport = value;
            }
        }

        public bool TcpNoDelay
        {
            get
            {
                return TcpTransport.TcpSocketClient.NoDelay;
            }
            set
            {
                TcpTransport.TcpSocketClient.NoDelay = value;
            }
        }

        /// <summary>
        /// Should the client allow Untrusted Certificates? See: <see href="https://cheatsheetseries.owasp.org/cheatsheets/Pinning_Cheat_Sheet.html"/>
        /// </summary>
        public bool AllowUntrustedRootCertificates { get; set; } = false;

        protected void ConfirmSSL()
        {
            Log.Success("SSL Succeeded.");
            TcpTransport.SetSSLState(true);
        }

        public bool ClientSSLUpgrade(string hostname)
        {
            return TcpTransport.ClientSSLUpgrade(hostname);
        }

        public bool ServerSSLUpgrade(X509Certificate certificate)
        {
            return TcpTransport.ServerSSLUpgrade(certificate);
        }

        public virtual bool ServerVerifyCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None || sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
            {
                return true;
            }
            Log.Error($"SSL Policy Errors: {string.Join(", ", sslPolicyErrors.GetActiveFlags())}");
            return false;
        }

        public virtual bool ClientVerifyCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                TcpTransport.Certificate = certificate;
                return true;
            }
            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
            {
                X509ChainStatusFlags flags = 0;
                foreach (X509ChainStatus chainEntry in chain.ChainStatus)
                {
                    flags |= chainEntry.Status;
                }
                if (flags.HasFlag(X509ChainStatusFlags.UntrustedRoot))
                {
                    if (AllowUntrustedRootCertificates)
                    {
                        Log.Warning("Untrusted root certificate detected. However, this client accepts this. Continue at your own risk!");
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            Log.Error($"SSL Policy Errors: {string.Join(", ", sslPolicyErrors.GetActiveFlags())}");
            return false;
        }

        protected bool ClientTrySSLUpgrade()
        {
            Log.Info($"Trying to upgrade this TCP/IP connection to SSL...");
            bool result = ClientSSLUpgrade(ConnectedHostname);
            if (!result)
            {
                Log.Info("SSL Failure");
            }
            return result;
        }

        protected bool ServerTrySSLUpgrade()
        {
            Log.Info($"Trying to upgrade this TCP/IP connection to SSL on Client {ClientID}...");
            bool result = ServerSSLUpgrade(NetworkServer.Config.Certificate);
            if (!result)
            {
                Log.Info($"SSL Failure, disconnecting client...");
            }
            return result;
        }


        protected override void HandleLocalClient(PacketHeader header, byte[] data)
        {
            switch (header.Type)
            {
                case PacketType.SSLUpgrade:
                    SSLUpgradePacket ssLUpgradePacket = new SSLUpgradePacket();
                    ssLUpgradePacket.Deserialize(data);
                    if (ssLUpgradePacket.Continue)
                    {
                        ConfirmSSL();
                        NoPacketSending = false;
                        break;
                    }
                    else
                    {
                        NoPacketSending = true;
                        bool attemptResult = ClientTrySSLUpgrade();
                        SSLUpgradePacket upgradePacketResult = new SSLUpgradePacket()
                        {
                            Result = attemptResult,
                        };
                        if (!attemptResult)
                        {
                            NoPacketSending = false;
                            Disconnect("SSL Handshake failure");
                        }
                        SendImmediate(upgradePacketResult);
                    }
                    return;
                default:
                    base.HandleLocalClient(header, data);
                    break;
            }
        }

        protected override void HandleRemoteClient(PacketHeader header, byte[] data)
        {
            switch (header.Type)
            {
                case PacketType.ClientData:
                    ClientDataPacket clientDataPacket = new ClientDataPacket();
                    clientDataPacket.Deserialize(data);
                    if (clientDataPacket.Configuration.Protocol != NetworkServer.ServerConfiguration.Protocol)
                    {
                        Disconnect($"Server protocol mismatch. Expected: {NetworkServer.ServerConfiguration.Protocol} Got: {clientDataPacket.Configuration.Protocol}");
                        break;
                    }
                    if (clientDataPacket.Configuration.Version != NetworkServer.ServerConfiguration.Version)
                    {
                        Disconnect($"Server protocol mismatch. Expected: {NetworkServer.ServerConfiguration.Version} Got: {clientDataPacket.Configuration.Version}");
                        break;
                    }
                    ServerDataPacket serverDataPacket = new ServerDataPacket
                    {
                        YourClientID = _clientId,
                        Configuration = NetworkServer.ServerConfiguration,
                        UpgradeToSSL = NetworkServer.Config.Certificate != null && SupportsSSL,
                    };
                    RemoteAddHeaders(serverDataPacket);
                    SendImmediate(serverDataPacket);
                    //ServerSyncPackets();
                    if (serverDataPacket.UpgradeToSSL && SupportsSSL)
                    {
                        NoPacketSending = true;
                        SSLUpgradePacket upgradePacket = new SSLUpgradePacket();
                        SendImmediate(upgradePacket);
                        ServerTrySSLUpgrade();
                    }
                    else
                    {
                        if (NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Required)
                        {
                            ServerBeginEncryption();
                        }
                        else if (NetworkServer.Config.DefaultReady && NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Disabled)
                        {
                            Ready = true;
                        }
                    }
                    CurrentConnectionState = ConnectionState.Connected;
                    InvokeClientIdUpdated();
                    return;
                case PacketType.SSLUpgrade:
                    SSLUpgradePacket sslUpgradePacket = new SSLUpgradePacket();
                    sslUpgradePacket.Deserialize(data);
                    if (sslUpgradePacket.Result)
                    {
                        SSLUpgradePacket sslUpgradeResponse = new SSLUpgradePacket()
                        {
                            Continue = true,
                        };
                        SendImmediate(sslUpgradeResponse);
                        ConfirmSSL();
                        NoPacketSending = false;
                        if (NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Required)
                        {
                            ServerBeginEncryption();
                        }
                        else if (NetworkServer.Config.DefaultReady && NetworkServer.Config.EncryptionMode == ServerEncryptionMode.Disabled)
                        {
                            Ready = true;
                        }
                    }
                    else
                    {
                        Disconnect("SSL Handshake failure");
                    }
                    return;
                default:
                    base.HandleRemoteClient(header, data);
                    break;
            }
        }
    }
}
