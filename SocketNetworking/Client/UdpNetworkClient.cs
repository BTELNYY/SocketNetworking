using SocketNetworking.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Client
{
    public class UdpNetworkClient : NetworkClient
    {
        public override NetworkTransport Transport
        {
            get
            {
                return base.Transport;
            }
            set
            {
                if (value is UdpTransport tcp)
                {
                    base.Transport = tcp;
                }
                else
                {
                    throw new InvalidOperationException("UdpNetworkClient does not support non-udp transport.");
                }
            }
        }

        public UdpTransport UdpTransport
        {
            get
            {
                return (UdpTransport)Transport;
            }
            set
            {
                Transport = value;
            }
        }

        protected override void PacketReaderThreadMethod()
        {
            while (true)
            {
                if (_shuttingDown)
                {   
                    break;
                }
                if (!IsConnected)
                {
                    StopClient();
                    return;
                }
                (byte[], Exception, IPEndPoint) packet = UdpTransport.Receive(0, 0);
                HandlePacket(packet.Item1, packet.Item3);
            }
        }
    }
}
