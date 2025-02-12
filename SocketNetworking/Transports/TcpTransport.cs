using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem;
using SocketNetworking.Server;
using SocketNetworking.Shared;

namespace SocketNetworking.Transports
{
    public class TcpTransport : NetworkTransport
    {
        public TcpTransport()
        {
            buffer = new byte[BufferSize];
        }

        public TcpTransport(TcpClient client) : this()
        {
            Client = client;
        }

        public X509Certificate Certificate { get; set; }

        public bool UsingSSL { get; private set; } = false;

        public override IPEndPoint Peer => Client.Client.RemoteEndPoint as IPEndPoint;

        public override IPEndPoint LocalEndPoint => Client.Client.LocalEndPoint as IPEndPoint;

        public override IPAddress PeerAddress => Peer.Address;

        public override int PeerPort => Peer.Port;

        public override bool IsConnected => Client.Connected;

        public SslStream SslStream { get; set; }

        public void SetSSLState(bool state)
        {
            lock(_lock)
            {
                if (SslStream == null)
                {
                    Log.GlobalError("SSL Stream is null when trying to set SSL state!");
                    return;
                }
                UsingSSL = true;
            }
        }

        public Stream Stream
        {
            get
            {
                if(UsingSSL)
                {
                    return SslStream;
                }
                return Client.GetStream();
            }
        }

        public TcpClient Client { get; set; } = new TcpClient();

        public override Socket Socket => Client.Client;

        public override bool DataAvailable
        {
            get
            {
                return Stream.CanRead && Socket.Available > 0;
            }
        }

        public override int DataAmountAvailable => Socket.Available;

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

        public override (byte[], Exception, IPEndPoint) Receive()
        {
            try
            {
                lock(_lock)
                {
                    Buffer = ReceiveInternal();
                }
                return (Buffer, null, Peer);
            }
            catch (Exception ex)
            {
                return (null, ex, Peer);
            }
        }

        [Obsolete("Use Receive(int, int) instead.")]
        public (byte[], Exception, IPEndPoint) ClassicReceive(int offset, int size)
        {
            try
            {
                Stream.Read(Buffer, offset, size);
                //Buffer = Receive();
                return (Buffer, null, Peer);
            }
            catch (Exception ex)
            {
                return (null, ex, Peer);
            }
        }

        object _lock = new object();

        int fillSize = 0;

        byte[] buffer = new byte[Packet.MaxPacketSize];

        /// <summary>
        /// Attempts to read a full packet. (this blocks the TCP connection until it can be read)
        /// </summary>
        /// <returns></returns>
        private byte[] ReceiveInternal()
        {
            while(true)
            {  
                if (!IsConnected)
                {
                    Log.GlobalWarning("Tcp Transport is not connecting but is trying to read.");
                    break;
                }
                if (fillSize < sizeof(int))
                {
                    // we dont have enough data to read the length data
                    int count = 0;
                    int tempFillSize = fillSize;
                    if (Client.NoDelay)
                    {
                        count = Stream.Read(buffer, 0, buffer.Length - fillSize);
                    }
                    else
                    {
                        count = Stream.Read(buffer, fillSize, buffer.Length - fillSize);
                    }
                    fillSize += count;
                    continue;
                }
                int bodySize = BitConverter.ToInt32(buffer, 0); // i sure do hope this doesnt modify the buffer.
                bodySize = IPAddress.NetworkToHostOrder(bodySize);
                //Log.GlobalDebug($"{bodySize}");
                if (bodySize == 0)
                {
                    fillSize = 0;
                    continue;
                }
                //if(bodySize > DataAmountAvailable)
                //{
                //    Log.GlobalError("Packet is larger than the amount of bytes sent over the network in the current stream!");
                //    break;
                //}
                fillSize -= sizeof(int); // this kinda desyncs fillsize from the actual size of the buffer, but eh
                                         // read the rest of the whole packet
                if (bodySize > Packet.MaxPacketSize || bodySize < 0)
                {
                    
                    string s = string.Empty;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        s += Convert.ToString(buffer[i], 2).PadLeft(8, '0') + " ";
                    }
                    //Log.GlobalError("Body Size is corrupted! Raw: " + s);
                }
                while (fillSize < bodySize)
                {
                    //Log.GlobalDebug($"Trying to read bytes to read the body (we need at least {bodySize} and we have {fillSize})!");
                    if (fillSize == buffer.Length)
                    {
                        // The buffer is too full, and we are fucked (oh shit)
                        Log.GlobalError("Buffer became full before being able to read an entire packet. This probably means a packet was sent that was bigger then the buffer (Which is the packet max size). This is not recoverable, Disconnecting!");
                        break;
                    }
                    int count;
                    count = Stream.Read(buffer, fillSize, buffer.Length - fillSize);
                    fillSize += count;
                }
                // we now know we have enough bytes to read at least one whole packet;
                byte[] fullPacket = Utils.ShiftOut(ref buffer, bodySize + sizeof(int));
                if ((fillSize -= bodySize) < 0)
                {
                    fillSize = 0;
                }
                return fullPacket;
            }
            return null;
        }

        public override Exception Send(byte[] data, IPEndPoint destination)
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

        public override Exception Send(byte[] data)
        {
            return Send(data, Peer);
        }

        public override void Close()
        {
            Client.Close();
        }
    }
}
