using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Shared.PacketSystem;

namespace SocketNetworking.Shared.Transports
{
    public class TcpTransport : NetworkTransport
    {
        /// <summary>
        /// This property is not supported on the <see cref="TcpTransport"/> as there is no buffer to set for the <see cref="NetworkTransport"/>. To modify actual buffer sizes, see <see cref="TcpClient.ReceiveBufferSize"/> and <see cref="TcpClient.SendBufferSize"/>. The <see cref="TcpClient"/> can be found at <see cref="TcpSocketClient"/>.
        /// </summary>
        public override int BufferSize { get => base.BufferSize; set => base.BufferSize = value; }

        public TcpTransport()
        {

        }

        public TcpTransport(TcpClient client) : this()
        {
            TcpSocketClient = client;
        }

        public X509Certificate Certificate { get; set; }

        public bool UsingSSL { get; private set; } = false;

        public override IPEndPoint Peer => TcpSocketClient.Client.RemoteEndPoint as IPEndPoint;

        public override IPEndPoint LocalEndPoint => TcpSocketClient.Client.LocalEndPoint as IPEndPoint;

        public override IPAddress PeerAddress => Peer.Address;

        public override int PeerPort => Peer.Port;

        public override bool IsConnected => TcpSocketClient != null && TcpSocketClient.Connected;

        public SslStream SslStream { get; set; }

        public void SetSSLState(bool state)
        {
            //what the fuck is this
            lock (_readLock)
            {
                lock (_writeLock)
                {
                    if (SslStream == null)
                    {
                        Log.GlobalError("SSL Stream is null when trying to set SSL state!");
                        return;
                    }
                    UsingSSL = state;
                }
            }
            if (state)
            {
                SSLConnected?.Invoke();
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
                return TcpSocketClient?.GetStream();
            }
        }

        public TcpClient TcpSocketClient { get; set; } = new TcpClient();

        public TcpNetworkClient TcpClient => Client as TcpNetworkClient;

        public override Socket Socket => TcpSocketClient?.Client;

        public override bool DataAvailable
        {
            get
            {
                return Stream != null && Stream.CanRead && DataAmountAvailable > 0;
            }
        }

        /// <summary>
        /// Called when SSL has finished its handshake and is now the standard transmission route.
        /// </summary>
        public event Action SSLConnected;

        /// <summary>
        /// Called when SSL has failed to authenticate.
        /// </summary>
        public event Action SSLFailure;

        public bool ClientSSLUpgrade(string hostname)
        {
            try
            {
                SslStream stream = new SslStream(Stream, true, TcpClient.ClientVerifyCert);
                stream.AuthenticateAsClient(hostname);
                SslStream = stream;
            }
            catch (AuthenticationException ex)
            {
                Client.Log.Error("SSL Authentication failure! Error: " + ex.Message);
                SSLFailure?.Invoke();
                return false;
            }
            catch (Exception ex)
            {
                Client.Log.Error("SSL General failure! Error: " + ex.ToString());
                SSLFailure?.Invoke();
                return false;
            }
            return true;
        }

        public bool ServerSSLUpgrade(X509Certificate certificate)
        {
            try
            {
                SslStream stream = new SslStream(Stream, true, TcpClient.ServerVerifyCert);
                stream.AuthenticateAsServer(certificate, false, SslProtocols.Tls12, true);
                //stream.AuthenticateAsServer(NetworkServer.Config.Certificate, false, true);
                SslStream = stream;
            }
            catch (AuthenticationException ex)
            {
                Client.Log.Error("SSL Authentication failure! Error: " + ex.Message);
                SSLFailure?.Invoke();
                return false;
            }
            catch (Exception ex)
            {
                Client.Log.Error("SSL General failure! Error: " + ex.ToString());
                SSLFailure?.Invoke();
                return false;
            }
            Certificate = certificate;
            return true;
        }


        public override int DataAmountAvailable => Socket == null ? 0 : Socket.Available;

        public override Exception Connect(string hostname, int port)
        {
            if (TcpSocketClient == null)
            {
                TcpSocketClient = new TcpClient();
            }
            try
            {
                TcpSocketClient.Connect(hostname, port);
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
                lock (_readLock)
                {
                    Buffer = ReceiveInternal();
                    //Log.GlobalDebug($"READ PACKET: SIZE: {Buffer.Length}, HASH: {Buffer.GetHashSHA1()}");
                    //if (Buffer.Length > 500)
                    //{
                    //    Log.GlobalDebug(Buffer.ByteArrayToString());
                    //}
                    ReceivedBytes += (ulong)Buffer.Length;
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
                ReceivedBytes += (ulong)Stream.Read(Buffer, offset, size);
                //Buffer = Receive();
                return (Buffer, null, Peer);
            }
            catch (Exception ex)
            {
                return (null, ex, Peer);
            }
        }

        readonly object _readLock = new object();

        readonly object _writeLock = new object();

        byte[] buffer = new byte[Packet.MaxPacketSize];

        readonly byte[] packetSizeBuffer = new byte[4];

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
                if (DataAmountAvailable >= 4)
                {
                    Stream.Read(packetSizeBuffer, 0, packetSizeBuffer.Length);
                    int bodySize = BitConverter.ToInt32(packetSizeBuffer, 0);
                    bodySize = IPAddress.NetworkToHostOrder(bodySize);
                    if (bodySize > Packet.MaxPacketSize)
                    {
                        break;
                    }
                    buffer = new byte[bodySize + 4];
                    for (int i = 0; i < packetSizeBuffer.Length; i++)
                    {
                        buffer[i] = packetSizeBuffer[i];
                    }
                    int curSize = 4;
                    while (curSize < buffer.Length)
                    {
                        int result = Stream.ReadByte();
                        if (result == -1)
                        {
                            Log.GlobalError("End of _stream");
                            break;
                        }
                        byte data = (byte)result;
                        buffer[curSize] = data;
                        curSize++;
                    }
                    return buffer;
                }
            }
            return null;
        }

        public override Exception Send(byte[] data, IPEndPoint destination)
        {
            try
            {
                lock (_writeLock)
                {
                    SentBytes += (ulong)data.Length;
                    Stream.Write(data, 0, data.Length);
                    return null;
                }
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
            TcpSocketClient?.Close();
            TcpSocketClient?.Dispose();
            TcpSocketClient = null;
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
            return await Task.Run(Receive);
        }

        public override async Task<Exception> ConnectAsync(string hostname, int port)
        {
            if (TcpSocketClient == null)
            {
                TcpSocketClient = new TcpClient();
            }
            try
            {
                await TcpSocketClient.ConnectAsync(hostname, port);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public override async Task CloseAsync()
        {
            await Task.Run(Close);
        }
    }
}
