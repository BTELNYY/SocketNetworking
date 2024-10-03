using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Transports
{
    public class UdpTransport : NetworkTransport
    {
        public override IPEndPoint Peer => Client.Client.RemoteEndPoint as IPEndPoint;

        public override IPEndPoint LocalEndPoint => Client.Client.LocalEndPoint as IPEndPoint;

        public override IPAddress PeerAddress => Peer.Address;

        public override int PeerPort => Peer.Port;

        public override bool IsConnected => Client.Client.Connected;

        public IPEndPoint BroadcastEndpoint
        {
            get
            {
                return new IPEndPoint(0, 0);
            }
        }

        public bool AllowBroadcast
        {
            get
            {
                return Client.EnableBroadcast;
            }
            set
            {
                Client.EnableBroadcast = value;
            }
        }

        public UdpClient Client { get; set; } = new UdpClient();

        private List<IPAddress> _multiCastGroups = new List<IPAddress>();

        public void JoinMulticastGroup(IPAddress multicastGroup)
        {
            _multiCastGroups.Add(multicastGroup);
            Client.JoinMulticastGroup(multicastGroup);
        }

        public void DropMulticastGroup(IPAddress multicastGroup)
        {
            _multiCastGroups.Remove(multicastGroup);
            Client.DropMulticastGroup(multicastGroup);
        }

        public bool InMulticastGroup(IPAddress multicastGroup)
        {
            return _multiCastGroups.Contains(multicastGroup);
        }

        public override void Close()
        {
            Client.Close();
        }

        public override Exception Connect(string hostname, int port)
        {
            try
            {
                Client.Connect(hostname, port);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public override (byte[], Exception, IPEndPoint) Receive(int offset, int size)
        {
            try
            {
                byte[] read = new byte[] { };
                IPEndPoint peer;
                if (AllowBroadcast)
                {
                    peer = BroadcastEndpoint;
                    read = Client.Receive(ref peer);
                }
                else
                {
                    peer = Peer;
                    read = Client.Receive(ref peer);
                }
                return (read, null, peer);
            }
            catch (Exception ex)
            {
                return (null, ex, null);
            }
        }

        public override Exception Send(byte[] data, IPEndPoint destination)
        {
            try
            {
                Client.Send(data, data.Length, destination);
                return null;
            }
            catch(Exception ex)
            {
                return ex;
            }
        }

        public override Exception Send(byte[] data)
        {
            return Send(data, Peer);
        }

        public Exception SendBroadcast(byte[] data)
        {
            try
            {
                Client.Send(data, data.Length, BroadcastEndpoint);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
