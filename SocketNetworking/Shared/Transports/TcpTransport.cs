using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SocketNetworking.Shared.PacketSystem;

namespace SocketNetworking.Shared.Transports
{
    public class TcpTransport : NetworkTransport
    {
        /// <summary>
        /// This property is not supported on the <see cref="TcpTransport"/> as there is no buffer to set for the <see cref="NetworkTransport"/>. To modify actual buffer sizes, see <see cref="TcpClient.ReceiveBufferSize"/> and <see cref="TcpClient.SendBufferSize"/>. The <see cref="TcpClient"/> can be found at <see cref="Client"/>.
        /// </summary>
        public override int BufferSize { get => base.BufferSize; set => base.BufferSize = value; }

        public TcpTransport()
        {
            
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

        public override bool IsConnected => Client != null && Client.Connected;

        public SslStream SslStream { get; set; }

        public void SetSSLState(bool state)
        {
            lock (_lock)
            {
                if (SslStream == null)
                {
                    Log.GlobalError("SSL Stream is null when trying to set SSL state!");
                    return;
                }
                UsingSSL = state;
            }
        }

        public Stream Stream
        {
            get
            {
                if (!IsConnected)
                {
                    return null;
                }
                if (UsingSSL)
                {
                    return SslStream;
                }
                return Client?.GetStream();
            }
        }

        public TcpClient Client { get; set; } = new TcpClient();

        public override Socket Socket => Client?.Client;

        public override bool DataAvailable
        {
            get
            {
                return Stream != null && Stream.CanRead && DataAmountAvailable > 0;
            }
        }

        public override int DataAmountAvailable => Socket == null ? 0 : Socket.Available;

        public override Exception Connect(string hostname, int port)
        {
            if (Client == null)
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
                lock (_lock)
                {
                    Buffer = ReceiveInternal();
                    //Log.GlobalDebug($"READ PACKET: SIZE: {Buffer.Length}, HASH: {Buffer.GetHashSHA1()}");
                    //if (Buffer.Length > 500)
                    //{
                    //    Log.GlobalDebug(Buffer.ByteArrayToString());
                    //}
                }
                return (Buffer, null, Peer);
            }
            catch (Exception ex)
            {
                return (null, ex, Peer);
            }
        }

        [Obsolete("Use Receive() instead.")]
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

        readonly object _lock = new object();

        byte[] buffer = new byte[Packet.MaxPacketSize];

        byte[] packetSizeBuffer = new byte[4];

        /// <summary>
        /// Attempts to read a full packet. (this blocks the TCP connection until it can be read)
        /// </summary>
        /// <returns></returns>
        private byte[] ReceiveInternal()
        {
            while (true)
            {
                if (!IsConnected)
                {
                    Log.GlobalWarning("Tcp Transport is not connecting but is trying to read.");
                    break;
                }
                //Log.GlobalDebug($"Stream Amount available: " + DataAmountAvailable);
                if (DataAmountAvailable >= 4)
                {
                    //Read the size.
                    Stream.Read(packetSizeBuffer, 0, packetSizeBuffer.Length);
                    int bodySize = BitConverter.ToInt32(packetSizeBuffer, 0); // i sure do hope this doesn't modify the buffer.
                    bodySize = IPAddress.NetworkToHostOrder(bodySize);
                    //Log.GlobalDebug("Read Size: " +  bodySize);
                    if (bodySize > Packet.MaxPacketSize)
                    {
                        break;
                    }
                    while (DataAmountAvailable < bodySize)
                    {
                        //Log.GlobalDebug($"Not enough data for the full packet, waiting. BodySize: {bodySize}, Amount ready: {DataAmountAvailable}");
                        //wait for full packet.
                    }
                    //Full packet + size
                    buffer = new byte[bodySize + 4];
                    //Place the size into the buffer
                    for (int i = 0; i < packetSizeBuffer.Length; i++)
                    {
                        buffer[i] = packetSizeBuffer[i];
                    }
                    //Offset the bytes
                    int read = Stream.Read(buffer, 4, bodySize);
                    if (read != bodySize)
                    {
                        throw new InvalidOperationException($"Didn't read all the bytes for the body size, or read to many! Read: {read}, BodySize: {bodySize}");
                    }
                    //Log.GlobalDebug("Read: " + read);
                    return buffer;
                }
                //    if (fillSize < sizeof(int))
                //    {
                //        // we don't have enough data to read the length data
                //        int count;
                //        if (Client.NoDelay)
                //        {
                //            count = Stream.Read(buffer, 0, buffer.Length - fillSize);
                //        }
                //        else
                //        {
                //            count = Stream.Read(buffer, fillSize, buffer.Length - fillSize);
                //        }
                //        fillSize += count;
                //        continue;
                //    }
                //    int bodySize = BitConverter.ToInt32(buffer, 0); // i sure do hope this doesn't modify the buffer.
                //    bodySize = IPAddress.NetworkToHostOrder(bodySize);
                //    //Log.GlobalDebug($"{bodySize}");
                //    if (bodySize == 0)
                //    {
                //        fillSize = 0;
                //        continue;
                //    }
                //    //if(bodySize > DataAmountAvailable)
                //    //{
                //    //    Log.GlobalError("Packet is larger than the amount of bytes sent over the network in the current stream!");
                //    //    break;
                //    //}
                //    fillSize -= sizeof(int); // this kinda desyncs fillsize from the actual size of the buffer, but eh
                //                             // read the rest of the whole packet
                //    if (bodySize > Packet.MaxPacketSize || bodySize < 0)
                //    {
                //        string s = string.Empty;
                //        for (int i = 0; i < buffer.Length; i++)
                //        {
                //            s += Convert.ToString(buffer[i], 2).PadLeft(8, '0') + " ";
                //        }
                //        //Log.GlobalError("Body Size is corrupted! Raw: " + s);
                //    }
                //    while (fillSize < bodySize)
                //    {
                //        //Log.GlobalDebug($"Trying to read bytes to read the body (we need at least {bodySize} and we have {fillSize})!");
                //        if (fillSize == buffer.Length)
                //        {
                //            // The buffer is too full, and we are fucked (oh shit)
                //            throw new InvalidOperationException("Buffer became full before being able to read an entire packet. This probably means a packet was sent that was bigger then the buffer (Which is the packet max size). This is not recoverable, Disconnecting!");
                //        }
                //        int count;
                //        count = Stream.Read(buffer, fillSize, buffer.Length - fillSize);
                //        fillSize += count;
                //    }
                //    // we now know we have enough bytes to read at least one whole packet;
                //    byte[] fullPacket = Utils.ShiftOut(ref buffer, bodySize + sizeof(int));
                //    if ((fillSize -= bodySize) < 0)
                //    {
                //        fillSize = 0;
                //    }
                //    return fullPacket;
            }
            return null;
        }

        public override Exception Send(byte[] data, IPEndPoint destination)
        {
            try
            {
                //Log.GlobalDebug($"SEND: SIZE: {data.Length}, HASH: {data.GetHashSHA1()}");
                //if (data.Length > 500)
                //{
                //    Log.GlobalDebug(data.ByteArrayToString());
                //}
                Stream.Write(data, 0, data.Length);
                Stream.Flush();
                //Thread.Sleep(1);
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
            Client?.Close();
            Client?.Dispose();
            Client = null;
        }

        public async override Task<Exception> SendAsync(byte[] data, IPEndPoint destination)
        {
            return await SendAsync(data);
        }

        public async override Task<Exception> SendAsync(byte[] data)
        {
            try
            {
                await Stream.WriteAsync(data, 0, data.Length);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public async override Task<(byte[], Exception, IPEndPoint)> ReceiveAsync()
        {
            return Receive();
        }
    }
}
