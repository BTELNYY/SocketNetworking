using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking.Transports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Client
{
    public class TcpNetworkClient : NetworkClient
    {
        public TcpNetworkClient()
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
                if(value is TcpTransport tcp)
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
                return TcpTransport.Client.NoDelay;
            }
            set
            {
                TcpTransport.Client.NoDelay = value;
            }
        }

        /// <summary>
        /// Should the client allow Untrusted Certificates? See: <see href="https://cheatsheetseries.owasp.org/cheatsheets/Pinning_Cheat_Sheet.html"/>
        /// </summary>
        public bool AllowUntrustedRootCertificates { get; set; } = false;

        /// <summary>
        /// Called when SSL has finished its handshake and is now the standard tranmission route.
        /// </summary>
        public event Action SSLConnected;

        /// <summary>
        /// Called when SSL has failed to authenticate.
        /// </summary>
        public event Action SSLFailure;

        protected override void ConfirmSSL()
        {
            Log.Success("SSL Succeeded.");
            TcpTransport.SetSSLState(true);
            SSLConnected?.Invoke();
        }

        public bool ClientSSLUpgrade(string hostname)
        {
            try
            {
                var stream = new SslStream(TcpTransport.Stream, true, ClientVerifyCert);
                stream.AuthenticateAsClient(hostname);
                TcpTransport.SslStream = stream;
            }
            catch (AuthenticationException ex)
            {
                SSLFailure?.Invoke();
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("SSL General failure! Error: " + ex.ToString());
                SSLFailure?.Invoke();
                return false;
            }
            return true;
        }

        public bool ServerSSLUpgrade(X509Certificate certificate)
        {
            try
            {
                var stream = new SslStream(TcpTransport.Stream, true, ServerVerifyCert);
                stream.AuthenticateAsServer(NetworkServer.Config.Certificate, false, true);
                TcpTransport.SslStream = stream;
            }
            catch (AuthenticationException ex)
            {
                SSLFailure?.Invoke();
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("SSL General failure! Error: " + ex.ToString());
                SSLFailure?.Invoke();
                return false;
            }
            TcpTransport.Certificate = certificate;
            return true;
        }

        protected virtual bool ServerVerifyCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None || sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
            {
                return true;
            }
            Log.Error($"SSL Policy Errors: {string.Join(", ", sslPolicyErrors.GetActiveFlags())}");
            return false;
        }

        protected virtual bool ClientVerifyCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                TcpTransport.Certificate = certificate;
                return true;
            }
            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
            {
                X509ChainStatusFlags flags = 0;
                foreach (var chainEntry in chain.ChainStatus)
                {
                    flags |= chainEntry.Status;
                }
                if (flags.HasFlag(X509ChainStatusFlags.UntrustedRoot))
                {
                    if(AllowUntrustedRootCertificates)
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
