using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
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

        protected override void DoSSLUpgrade(ServerDataPacket packet)
        {
            
        }
    }
}
