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
        }

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
                return;
            }
            lock (streamLock)
            {
                _toSendPackets.TryDequeue(out Packet packet);
                Log.GlobalDebug($"Active Flags: {string.Join(", ", packet.Flags.GetActiveFlags())}");
                PreparePacket(ref packet);
                Log.GlobalDebug($"Active Flags: {string.Join(", ", packet.Flags.GetActiveFlags())}");
                byte[] fullBytes = SerializePacket(packet);
                if(fullBytes == null)
                {
                    Log.GlobalDebug($"Packet dropped before sending, serialization failure. ID: {packet.CustomPacketID}, Type: {packet.GetType().FullName}, Destination: {packet.Destination.ToString()}");
                    return;
                }
                try
                {
                    Log.GlobalDebug($"Sending packet. Target: {packet.NetowrkIDTarget} Type: {packet.Type} CustomID: {packet.CustomPacketID} Length: {fullBytes.Length}");
                    Exception ex;
                    if (packet.Flags.HasFlag(PacketFlags.Priority))
                    {
                        ex = UdpTransport.Send(fullBytes, packet.Destination);
                    }
                    else
                    {
                        ex = Transport.Send(fullBytes, packet.Destination);
                    }
                    if (ex != null)
                    {
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    Log.GlobalError("Failed to send packet! Error:\n" + ex.ToString());
                    NetworkErrorData networkErrorData = new NetworkErrorData("Failed to send packet: " + ex.ToString(), true);
                    InvokeConnectionError(networkErrorData);
                }
            }
        }

        private Thread _udpThread = null;

        public Thread UdpThread
        {
            get
            {
                if(_udpThread == null)
                {
                    _udpThread = new Thread(UdpReaderThread);
                }
                return _udpThread;
            }
        }

        void UdpReaderThread()
        {
            while (true)
            {
                if (_shuttingDown)
                {
                    break;
                }
                if (!UdpTransport.IsConnected)
                {
                    continue;
                }
                (byte[], Exception, IPEndPoint) packet = UdpTransport.Receive(0, 0);
                if(packet.Item2 != null)
                { 
                    Log.GlobalError(packet.Item2.ToString());
                    continue;
                }
                if(packet.Item1.Length == 0)
                {
                    continue;
                }
                Deserialize(packet.Item1, packet.Item3);
            }
        }

        public override void InitRemoteClient(int clientId, NetworkTransport socket)
        {
            base.ClientConnected += MixedNetworkClient_ClientConnected;
            base.InitRemoteClient(clientId, socket);
        }

        private void MixedNetworkClient_ClientConnected()
        {
            Random random = new Random();
            InitialUDPKey = random.Next(int.MinValue, int.MaxValue);
            ServerSendUDPInfo(InitialUDPKey);
            _udpThread = new Thread(UdpReaderThread);
            _udpThread.Start();
        }

        private void ServerSendUDPInfo(int passKey)
        {
            NetworkInvoke(nameof(ClientRecieveUDPInfo), new object[] { passKey });  
        }

        [NetworkInvocable(PacketDirection.Server)]
        private void ClientRecieveUDPInfo(int passKey)
        {
            Exception ex = UdpTransport.Connect(Transport.PeerAddress.ToString(), Transport.Peer.Port);
            if (ex != null)
            {
                Disconnect("[Client] UDP Connection Failed.");
            }
            else
            {
                InitialUDPKey = passKey;
                ByteWriter writer = new ByteWriter();
                writer.WriteInt(ClientID);
                writer.WriteInt(InitialUDPKey);
                _udpThread = new Thread(UdpReaderThread);
                _udpThread.Start();
                UdpTransport.Send(writer.Data);
            }
        }
    }
}
