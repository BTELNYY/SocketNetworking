using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketNetworking.Transports
{
    public class TcpTransport : NetworkTransport
    {
        public override IPEndPoint Peer => Client.Client.RemoteEndPoint as IPEndPoint;

        public override IPAddress PeerAddress => Peer.Address;

        public override int PeerPort => Peer.Port;

        public override bool IsConnected => Client.Connected;

        public NetworkStream Stream
        {
            get
            {
                return Client.GetStream();
            }
        }

        public TcpClient Client { get; set; } = new TcpClient();

        public override Exception Connect(string hostname, int port)
        {
            if(Client == null)
            {
                Client = new TcpClient();
            }
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
                Stream.Read(Buffer, offset, size);
                return (Buffer, null, Peer);
            }
            catch (Exception ex)
            {
                return (null, ex, Peer);
            }
        }

        public override Exception Send(byte[] data)
        {
            try
            {
                Stream.Write(data, 0, data.Length);
                Thread.Sleep(1);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }   
        }

        public override void Close()
        {
            Client.Close();
        }
    }
}
