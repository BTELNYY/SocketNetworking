using SocketNetworking.PacketSystem;
using SocketNetworking.Transports;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.Shared;
using System.Net.Sockets;
using System.Threading;

namespace SocketNetworking.Client
{
    public class MixedNetworkClient : TcpNetworkClient
    {
        public MixedNetworkClient()
        {
            Transport = new TcpTransport();
            _networkEncryptionManager = new NetworkEncryptionManager();
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
                if(value is TcpTransport tcpTransport)
                {
                    base.Transport = tcpTransport;
                }
                else if(value is UdpTransport udpTransport)
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
            if (_toSendPackets.IsEmpty)
            {
                //Log.Debug("Nothing to send.");
                return;
            }
            lock (streamLock)
            {
                _toSendPackets.TryDequeue(out Packet packet);
                PreparePacket(ref packet);
                //Log.Debug($"Active Flags: {string.Join(", ", packet.Flags.GetActiveFlags())}");
                if(packet.Flags.HasFlag(PacketFlags.Priority))
                {
                    packet.Destination = UdpTransport.Peer;
                }
                byte[] fullBytes = SerializePacket(packet);
                if(fullBytes == null)
                {
                    //Log.Debug($"Packet dropped before sending, serialization failure. ID: {packet.CustomPacketID}, Type: {packet.GetType().FullName}, Destination: {packet.Destination.ToString()}");
                    return;
                }
                try
                {
                    //Log.Debug($"Sending packet. Target: {packet.NetowrkIDTarget} Type: {packet.Type} CustomID: {packet.CustomPacketID} Length: {fullBytes.Length}");
                    Exception ex;
                    if (packet.Flags.HasFlag(PacketFlags.Priority))
                    {
                        ex = UdpTransport.Send(fullBytes, packet.Destination);
                    }
                    else
                    {
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
            try
            {
                Deserialize(packet.Item1, packet.Item3);
            }
            catch(Exception ex)
            {
                Log.Warning($"Malformed Packet. Length: {packet.Item1.Length}, From: {packet.Item2}");
            }
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
            NetworkInvoke(nameof(ClientRecieveUDPInfo), new object[] { passKey });  
        }

        [NetworkInvokable(NetworkDirection.Server)]
        private void ClientRecieveUDPInfo(int passKey)
        {
            if(_udpConnected)
            {
                return;
            }
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
