using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        protected override void ConfirmSSL()
        {
            Log.Success("SSL Succeeded.");
            TcpTransport.SetSSLState(true);
        }

        protected override bool ClientTrySSLUpgrade()
        {
            Log.Info($"Trying to upgrade this TCP/IP connection to SSL...");
            bool result = TcpTransport.ClientSSLUpgrade(ConnectedHostname);
            if (!result)
            {
                Log.Info("SSL Failure");
            }
            return result;
        }

        protected override bool ServerTrySSLUpgrade()
        {
            Log.Info($"Trying to upgrade this TCP/IP connection to SSL on Client {ClientID}...");
            bool result = TcpTransport.ServerSSLUpgrade(NetworkServer.Config.Certificate);
            if (!result)
            {
                Log.Info($"SSL Failure, disconnecting client...");
            }
            return result;
        }
    }
}
