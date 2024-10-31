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

namespace SocketNetworking.Client
{
    public class MixedNetworkClient : NetworkClient
    {
        public int InitialUDPKey = 0;

        public override NetworkTransport Transport
        {
            get
            {
                return Transport;
            }
            set
            {
                if(value is TcpTransport tcpTransport)
                {
                    Transport = tcpTransport;
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

        public override void InitRemoteClient(int clientId, NetworkTransport socket)
        {
            base.InitRemoteClient(clientId, socket);
            base.ClientConnected += MixedNetworkClient_ClientConnected;
        }

        private void MixedNetworkClient_ClientConnected()
        {
            Random random = new Random();
            InitialUDPKey = random.Next(int.MinValue, int.MaxValue);
            ServerSendUDPInfo(InitialUDPKey);
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
                UdpTransport.Send(writer.Data);
            }
        }
    }
}
