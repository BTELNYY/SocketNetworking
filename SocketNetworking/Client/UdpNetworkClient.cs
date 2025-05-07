using System;
using System.Net;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Transports;

namespace SocketNetworking.Client
{
    /// <summary>
    /// The <see cref="UdpNetworkClient"/> class is the client which uses the <see cref="Shared.Transports.UdpTransport"/> as the transport. It is not recommended to be used. Use <see cref="MixedNetworkClient"/> if you need UDP functionality.
    /// </summary>
    public class UdpNetworkClient : NetworkClient
    {
        public UdpNetworkClient()
        {
            Transport = new UdpTransport();
            _currentMode = DefaultMode;
        }

        /// <summary>
        /// Unused.
        /// </summary>
        public virtual UdpClientMode DefaultMode
        {
            get
            {
                return UdpClientMode.ServerClient;
            }
        }

        protected UdpClientMode _currentMode;

        /// <summary>
        /// Unused.
        /// </summary>
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

        /// <summary>
        /// <see cref="Transport"/> but casted to <see cref="Shared.Transports.UdpTransport"/>.
        /// </summary>
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

        /// <summary>
        /// Sends the <paramref name="packet"/> to the <paramref name="destination"/> by setting the <see cref="Packet.Destination"/> property to the <paramref name="destination"/>.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="destination"></param>
        public void Send(Packet packet, IPEndPoint destination)
        {
            packet.Destination = destination;
            Send(packet);
        }

        /// <summary>
        /// Sends the <paramref name="packet"/> to the <paramref name="destination"/> by setting the <see cref="Packet.Destination"/> property to the <paramref name="destination"/>. <paramref name="sender"/> is used to set the <see cref="TargetedPacket.NetworkIDTarget"/> property.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="sender"></param>
        /// <param name="destination"></param>
        public void Send(TargetedPacket packet, INetworkObject sender, IPEndPoint destination)
        {
            packet.NetworkIDTarget = sender.NetworkID;
            Send(packet, destination);
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
            DeserializeRetry(packet.Item1, packet.Item3);
        }
    }
}
