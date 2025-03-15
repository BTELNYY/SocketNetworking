using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Transports;
using System;
using System.Net;

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

        public void Send(TargetedPacket packet, INetworkObject sender, IPEndPoint peer)
        {
            packet.NetworkIDTarget = sender.NetworkID;
            Send(packet, peer);
        }

        protected override void RawReader()
        {
            if (!IsTransportConnected)
            {
                StopClient();
                return;
            }
            if (!UdpTransport.DataAvailable)
            {
                return;
            }
            (byte[], Exception, IPEndPoint) packet = Transport.Receive();
            if (packet.Item1 == null)
            {
                Log.Warning("Transport Received a null byte array.");
                return;
            }
            Deserialize(packet.Item1, packet.Item3);
        }
    }
}
