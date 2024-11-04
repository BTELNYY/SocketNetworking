using SocketNetworking.PacketSystem;
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
    public class UdpNetworkClient : NetworkClient
    {

        public UdpNetworkClient()
        {
            Transport = new UdpTransport();
            _currentMode = DefaultMode;
        }

        public virtual UdpClientMode DefaultMode
        {
            get
            {
                return UdpClientMode.ServerClient;
            }
        }

        protected UdpClientMode _currentMode;

        public virtual UdpClientMode ClientMode
        {
            get
            {
                return _currentMode;
            }
            set
            {
                _currentMode = value;
            }
        }


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

        public void Send(Packet packet, IPEndPoint where)
        {
            packet.Destination = where;
            Send(packet);
        }

        public void Send(Packet packet, INetworkObject sender, IPEndPoint peer)
        {
            packet.NetowrkIDTarget = sender.NetworkID;
            Send(packet, peer);
        }

        protected override void PacketReaderThreadMethod()
        {
            while (true)
            {
                if (_shuttingDown)
                {   
                    break;
                }
                if (!IsTransportConnected)
                {
                    StopClient();
                    return;
                }
                (byte[], Exception, IPEndPoint) packet = UdpTransport.Receive(0, 0);
                Deserialize(packet.Item1, packet.Item3);
            }
        }
    }
}
