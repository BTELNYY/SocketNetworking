using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        public void FlushBuffer()
        {
            Buffer = new byte[BufferSize];
        }

        public abstract IPEndPoint Peer { get; }

        public abstract IPAddress PeerAddress { get; }

        public abstract int PeerPort { get; }

        public abstract bool IsConnected { get; }

        public abstract Exception Connect(string hostname, int port);

        public abstract Exception Send(byte[] data);

        public abstract (byte[], Exception) Receive(int offset, int size);

        public abstract void Close();
    }
}
