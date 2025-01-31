using SocketNetworking.PacketSystem;
using SocketNetworking.Shared;
using SocketNetworking.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Client
{
    public class TcpNetworkClient : NetworkClient
    {
        public TcpNetworkClient()
        {
            Transport = new TcpTransport();
        }

        public override NetworkTransport Transport
        {
            get
            {
                return base.Transport;
            }
            set
            {
                if(value is TcpTransport tcp)
                {
                    base.Transport = tcp;
                }
                else
                {
                    throw new InvalidOperationException("TcpNetworkClient does not support non-tcp transport.");
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

        public bool TcpNoDelay
        {
            get
            {
                return TcpTransport.Client.NoDelay;
            }
            set
            {
                TcpTransport.Client.NoDelay = value;
            }
        }

        public override void Init()
        {
            base.Init();
            buffer = new byte[Packet.MaxPacketSize];
            fillSize = 0;
        }

        byte[] buffer = new byte[Packet.MaxPacketSize];

        int fillSize = 0;

        protected override void RawReader()
        {
            base.RawReader();
            return;
            if (!Transport.DataAvailable)
            {
                //Log.Debug("No data available (TCP)");
                return;
            }
            Transport.BufferSize = Packet.MaxPacketSize;
            if (!IsTransportConnected)
            {
                Log.Debug("Disconnected!");
                StopClient();
                return;
            }
            if (fillSize < sizeof(int))
            {
                // we dont have enough data to read the length data
                int count = 0;
                try
                {
                    int tempFillSize = fillSize;
                    if (TcpNoDelay)
                    {
                        (byte[], Exception, IPEndPoint) transportRead = TcpTransport.ClassicReceive(0, buffer.Length - fillSize);
                        count = transportRead.Item1.Length;
                        buffer = Transport.Buffer;
                    }
                    else
                    {
                        (byte[], Exception, IPEndPoint) transportRead = TcpTransport.ClassicReceive(fillSize, buffer.Length - fillSize);
                        count = transportRead.Item1.Length;
                        buffer = Transport.Buffer;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return;
                }
                fillSize += count;
                return;
            }
            int bodySize = BitConverter.ToInt32(buffer, 0); // i sure do hope this doesnt modify the buffer.
            bodySize = IPAddress.NetworkToHostOrder(bodySize);
            if (bodySize == 0)
            {
                fillSize = 0;
                return;
            }
            fillSize -= sizeof(int); // this kinda desyncs fillsize from the actual size of the buffer, but eh
                                     // read the rest of the whole packet
            if (bodySize > Packet.MaxPacketSize || bodySize < 0)
            {
                CurrentConnectionState = ConnectionState.Disconnected;
                string s = string.Empty;
                for (int i = 0; i < buffer.Length; i++)
                {
                    s += Convert.ToString(buffer[i], 2).PadLeft(8, '0') + " ";
                }
                Log.Error("Body Size is corrupted! Raw: " + s);
            }
            while (fillSize < bodySize)
            {
                //Log.Debug($"Trying to read bytes to read the body (we need at least {bodySize} and we have {fillSize})!");
                if (fillSize == buffer.Length)
                {
                    // The buffer is too full, and we are fucked (oh shit)
                    Log.Error("Buffer became full before being able to read an entire packet. This probably means a packet was sent that was bigger then the buffer (Which is the packet max size). This is not recoverable, Disconnecting!");
                    Disconnect("Illegal Packet Size");
                    break;
                }
                int count;
                try
                {

                    (byte[], Exception, IPEndPoint) transportRead = TcpTransport.ClassicReceive(fillSize, buffer.Length - fillSize);
                    count = transportRead.Item1.Length;
                    buffer = Transport.Buffer;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return;
                }
                fillSize += count;
            }
            // we now know we have enough bytes to read at least one whole packet;
            byte[] fullPacket = Utils.ShiftOut(ref buffer, bodySize + sizeof(int));
            if ((fillSize -= bodySize) < 0)
            {
                fillSize = 0;
            }
            Deserialize(fullPacket, Transport.Peer);
        }
    }
}
