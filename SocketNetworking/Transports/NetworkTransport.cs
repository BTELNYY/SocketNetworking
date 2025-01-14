using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Transports
{
    public abstract class NetworkTransport
    {
        public NetworkTransport()
        {
            Buffer = new byte[BufferSize];
        }

        public int BufferSize { get; set; } = Packet.MaxPacketSize;

        public byte[] Buffer { get; private set; } = new byte[] { };

        public abstract bool DataAvailable { get; }

        public abstract int DataAmountAvailable { get; }

        public void FlushBuffer()
        {
            Buffer = new byte[BufferSize];
        }

        public abstract IPEndPoint Peer { get; }

        public abstract IPEndPoint LocalEndPoint { get; }

        public abstract IPAddress PeerAddress { get; }

        public abstract Socket Socket { get; }

        public abstract int PeerPort { get; }

        public abstract bool IsConnected { get; }

        public abstract Exception Connect(string hostname, int port);

        public abstract Exception Send(byte[] data, IPEndPoint destination);

        public abstract Exception Send(byte[] data);

        public abstract (byte[], Exception, IPEndPoint) Receive(int offset, int size);

        public abstract void Close();
    }
}
