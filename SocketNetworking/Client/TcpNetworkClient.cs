using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SocketNetworking.Server;
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

        public override bool SupportsSSL => true;

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

        protected override void ConfirmSSL()
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

        protected override bool ClientTrySSLUpgrade()
        {
            Log.Info($"Trying to upgrade this TCP/IP connection to SSL...");
            bool result = ClientSSLUpgrade(ConnectedHostname);
            if (!result)
            {
                Log.Info("SSL Failure");
            }
            return result;
        }

        protected override bool ServerTrySSLUpgrade()
        {
            Log.Info($"Trying to upgrade this TCP/IP connection to SSL on Client {ClientID}...");
            bool result = ServerSSLUpgrade(NetworkServer.Config.Certificate);
            if (!result)
            {
                Log.Info($"SSL Failure, disconnecting client...");
            }
            return result;
        }
    }
}
