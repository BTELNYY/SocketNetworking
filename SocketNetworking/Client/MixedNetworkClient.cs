using System;
using System.Net;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.Transports;

namespace SocketNetworking.Client
{
    /// <summary>
    /// The <see cref="MixedNetworkClient"/> class is responsible for handling communication with a <see cref="SocketNetworking.Shared.Transports.TcpTransport"/> and a <see cref="Shared.Transports.UdpTransport"/> as transports. Both transports are used to send data, but the <see cref="PacketFlags.Priority"/> flag must be flipped on <see cref="Packet"/>s in order for them to be sent via UDP.
    /// </summary>
    public class MixedNetworkClient : TcpNetworkClient
    {
        public MixedNetworkClient() : base()
        {
            Transport = new TcpTransport();
        }

        protected override void OnLocalStopClient()
        {
            UdpTransport?.Close();
            base.OnLocalStopClient();
        }

        protected override void OnRemoteStopClient()
        {
            UdpTransport?.Close();
            base.OnRemoteStopClient();
        }

        public int UDPFailures = 0;

        public int InitialUDPKey = 0;

        public override NetworkTransport Transport
        {
            get
            {
                return base.Transport;
            }
            set
            {
                if (value is TcpTransport tcpTransport)
                {
                    base.Transport = tcpTransport;
                }
                else if (value is UdpTransport udpTransport)
                {
                    UdpTransport = udpTransport;
                }
                else
                {
                    throw new ArgumentException("MixeClients only support TCP and UDP transports!");
                }
            }
        }

        public new TcpTransport TcpTransport
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

        private UdpTransport _transport = new UdpTransport();

        public UdpTransport UdpTransport
        {
            get
            {
                return _transport;
            }
            set
            {
                _transport = value;
            }
        }


        protected override void SendNextPacketInternal()
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (NoPacketSending)
            {
                return;
            }
            if (_toSendPackets.IsEmpty)
            {
                //Log.Debug("Nothing to send.");
                return;
            }
            lock (streamLock)
            {
                _toSendPackets.TryDequeue(out Packet packet);
                PreparePacket(ref packet);
                if (!InvokePacketSendRequest(packet))
                {
                    return;
                }
                if (packet.Flags.HasFlag(PacketFlags.Priority))
                {
                    packet.Destination = UdpTransport.Peer;
                }
                byte[] fullBytes = SerializePacket(packet);
                if (fullBytes == null)
                {
                    return;
                }
                try
                {
                    Exception ex;
                    if (packet.Flags.HasFlag(PacketFlags.Priority))
                    {
                        if (!UdpTransport.IsConnected)
                        {
                            return;
                        }
                        ex = UdpTransport.Send(fullBytes, packet.Destination);
                    }
                    else
                    {
                        if (!TcpTransport.IsConnected)
                        {
                            return;
                        }
                        ex = Transport.Send(fullBytes, packet.Destination);
                    }
                    //Log.Debug("Packet sent!");
                    if (ex != null)
                    {
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to send packet! Error:\n" + ex.ToString());
                    NetworkErrorData networkErrorData = new NetworkErrorData("Failed to send packet: " + ex.ToString(), true);
                    InvokeConnectionError(networkErrorData);
                }
                InvokePacketSent(packet);
            }
        }

        protected override void RawReader()
        {
            base.RawReader();
            if (_shuttingDown)
            {
                return;
            }
            if (!UdpTransport.IsConnected)
            {
                return;
            }
            if (!UdpTransport.DataAvailable)
            {
                return;
            }
            (byte[], Exception, IPEndPoint) packet = UdpTransport.Receive();
            if (packet.Item2 != null)
            {
                Log.Error(packet.Item2.ToString());
                return;
            }
            if (packet.Item1.Length == 0)
            {
                return;
            }
            DeserializeRetry(packet.Item1, packet.Item3);
        }

        bool _udpConnected = false;

        public override void InitRemoteClient(int clientId, NetworkTransport socket)
        {
            base.ClientIdUpdated += MixedNetworkClient_ClientIdUpdated;
            base.InitRemoteClient(clientId, socket);
        }

        private void MixedNetworkClient_ClientIdUpdated()
        {
            if (_udpConnected)
            {
                return;
            }
            Random random = new Random();
            InitialUDPKey = random.Next(int.MinValue, int.MaxValue);
            ServerSendUDPInfo(InitialUDPKey);
            _udpConnected = true;
        }

        private void ServerSendUDPInfo(int passKey)
        {
            NetworkInvoke(nameof(ClientReceiveUDPInfo), new object[] { passKey });
        }

        [NetworkInvokable(NetworkDirection.Server)]
        private void ClientReceiveUDPInfo(int passKey)
        {
            if (_udpConnected)
            {
                return;
            }
            Log.Info($"Connecting to {Transport.PeerAddress.ToString()}:{Transport.Peer.Port} on UDP...");
            Exception ex = UdpTransport.Connect(Transport.PeerAddress.ToString(), Transport.Peer.Port);
            if (ex != null)
            {
                Disconnect("[Client] UDP Connection Failed.");
            }
            else
            {
                _udpConnected = true;
                InitialUDPKey = passKey;
                ByteWriter writer = new ByteWriter();
                writer.WriteInt(ClientID);
                writer.WriteInt(InitialUDPKey);
                UdpTransport.Send(writer.Data);
            }
        }
    }
}
