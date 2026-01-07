#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;
using SocketNetworking.Shared.PacketSystem;

namespace SocketNetworking.Client
{
    [RequiresPreviewFeatures]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macOS")]
    public class QuicNetworkClient : NetworkClient
    {
        public const long DefaultErrorCode = 0x0A;

        public const long DefaultStreamClosedCode = 0x0B;

        public QuicConnection Connection { get; private set; }

        public QuicStream Stream { get; private set; }

        //Technically, quic does support SSL. But not live switching to it.
        public override bool SupportsSSL => false;

        public override bool IsConnected
        {
            get
            {
                if (Connection == null)
                {
                    return false;
                }
                if (Stream == null)
                {
                    return false;
                }
                return Stream.CanWrite && Stream.CanRead;
            }
            set
            {
                base.IsConnected = value;
            }
        }



        public override IPEndPoint ConnectedPeer => Connection.RemoteEndPoint;

        public override bool IsTransportConnected => IsConnected;

        public void SetupRemoteClient(QuicConnection connection)
        {
            Connection = connection;
            Task<QuicStream> tS = connection.AcceptInboundStreamAsync().AsTask();
            tS.Wait();
            Stream = tS.Result;
        }

        public override bool Connect(string hostname, ushort port)
        {
            return Task.Run(() => {
                return ConnectAsync(hostname, port);
            }).Result;
        }

        /// <summary>
        /// Client Connection options. This property will be changed on connection. See <see cref="OnPreConnect"/>,
        /// </summary>
        public QuicClientConnectionOptions ClientConnectionOptions { get; private set; } = new QuicClientConnectionOptions();

        /// <summary>
        /// Called right before a connection is made in <see cref="ConnectAsync(string, ushort)"/>, used to allow the developer to modify the connection options before the connection is made.
        /// </summary>
        public event Func<QuicClientConnectionOptions, QuicClientConnectionOptions> OnPreConnect;

        public async Task<bool> ConnectAsync(string hostname, ushort port)
        {
            if (CurrentClientLocation == ClientLocation.Remote)
            {
                Log.Error("Cannot connect to other servers from remote.");
                return false;
            }
            if (IsTransportConnected)
            {
                Log.Error("Can't connect: Already connected to a server.");
                return false;
            }
            if (!QuicConnection.IsSupported)
            {
                Log.Error("QUIC is not supported.");
                return false;
            }
            string finalHostname;
            if (!IPAddress.TryParse(hostname, out IPAddress ip))
            {
                try
                {
                    IPHostEntry entry = Dns.GetHostEntry(hostname);
                    if (entry.AddressList.Count() == 0)
                    {
                        Log.Error($"Can't find host {hostname}");
                        return false;
                    }
                    else
                    {
                        finalHostname = entry.AddressList[0].ToString();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("DNS Resolution failed. " + ex.ToString());
                    return false;
                }
            }
            else
            {
                finalHostname = hostname;
            }
            Log.Info($"Connecting to {finalHostname}:{port}...");
            ClientConnectionOptions = new QuicClientConnectionOptions()
            {
                RemoteEndPoint = IPEndPoint.Parse(finalHostname + ":" + port),
                DefaultCloseErrorCode = DefaultErrorCode,
                DefaultStreamErrorCode = DefaultStreamClosedCode,
                ClientAuthenticationOptions = new System.Net.Security.SslClientAuthenticationOptions()
                {
                    ApplicationProtocols =
                    [
                        new SslApplicationProtocol(ClientConfiguration.Protocol)
                    ],
                    TargetHost = hostname,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                    {
                        return true;
                    },
                }
            };
            if (OnPreConnect.GetInvocationList().Length > 0)
            {
                ClientConnectionOptions = OnPreConnect(ClientConnectionOptions);
            }
            try
            {
                QuicConnection connection = await QuicConnection.ConnectAsync(ClientConnectionOptions);
                QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                Connection = connection;
                Stream = stream;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
            return true;
        }

        public override void Disconnect(string message)
        {
            _ = DisconnectAsync(message);
        }

        public override async Task DisconnectAsync(string message)
        {
            await base.DisconnectAsync(message);
            await Connection.CloseAsync(0x0);
            return;
        }

        public override void SendImmediate(Packet packet)
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (NoPacketSending)
            {
                return;
            }
            if (_toSendPackets.IsEmpty)
            {
                return;
            }
            lock (streamLock)
            {
                PreparePacket(ref packet);
                if (!InvokePacketSendRequest(packet))
                {
                    return;
                }
                byte[] fullBytes = SerializePacket(packet);
                try
                {
                    Stream.Write(fullBytes, 0, fullBytes.Length);
                    _sentBytes += (ulong)fullBytes.Length;
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to send packet! Error:\n" + ex.ToString());
                    NetworkErrorData networkErrorData = new NetworkErrorData("Failed to send packet: " + ex.ToString(), true);
                    InvokeConnectionError(networkErrorData);
                }
                InvokePacketSent(packet);
            }
        }

        public override async Task SendImmediateAsync(Packet packet)
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (NoPacketSending)
            {
                return;
            }
            if (_toSendPackets.IsEmpty)
            {
                return;
            }
            PreparePacket(ref packet);
            if (!InvokePacketSendRequest(packet))
            {
                return;
            }
            byte[] fullBytes = SerializePacket(packet);
            try
            {
                await Stream.WriteAsync(fullBytes, 0, fullBytes.Length);
                _sentBytes += (ulong)fullBytes.Length;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to send packet! Error:\n" + ex.ToString());
                NetworkErrorData networkErrorData = new NetworkErrorData("Failed to send packet: " + ex.ToString(), true);
                InvokeConnectionError(networkErrorData);
            }
            InvokePacketSent(packet);
        }

        protected override void SendNextPacketInternal()
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (NoPacketSending)
            {
                return;
            }
            if (_toSendPackets.IsEmpty)
            {
                return;
            }
            lock (streamLock)
            {
                _toSendPackets.TryDequeue(out Packet packet);
                PreparePacket(ref packet);
                if (!InvokePacketSendRequest(packet))
                {
                    return;
                }
                byte[] fullBytes = SerializePacket(packet);
                try
                {
                    Stream.Write(fullBytes, 0, fullBytes.Length);
                    _sentBytes += (ulong)fullBytes.Length;
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to send packet! Error:\n" + ex.ToString());
                    NetworkErrorData networkErrorData = new NetworkErrorData("Failed to send packet: " + ex.ToString(), true);
                    InvokeConnectionError(networkErrorData);
                }
                InvokePacketSent(packet);
            }
        }

        private ulong _sentBytes = 0;

        public override ulong BytesSent => _sentBytes;

        private ulong _recievedBytes = 0;

        public override ulong BytesReceived => _recievedBytes;

        private IAsyncResult _readResult;

        protected override void RawReader()
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (!IsTransportConnected)
            {
                StopClient();
                return;
            }
            //Log.Debug("Do latency check!");
            DoLatencyCheck();
            if (!Transport.DataAvailable)
            {
                //Log.Debug("No data on transport!");
                return;
            }
            if (_readResult != null)
            {
                return;
            }
            _readResult = Stream.BeginRead(_headerBuffer, 0, 4, ReadStream, null);
        }

        protected override async Task RawReaderAsync()
        {
            if (NoPacketHandling)
            {
                return;
            }
            if (!IsTransportConnected)
            {
                StopClient();
                return;
            }
            //Log.Debug("Do latency check!");
            DoLatencyCheck();
            if (!Transport.DataAvailable)
            {
                //Log.Debug("No data on transport!");
                return;
            }
            await Stream.ReadAsync(_headerBuffer, 0, 4);
            int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_headerBuffer, 0));
            _buffer = new byte[length];
            await Stream.ReadAsync(_buffer, 0, length);
            byte[] fullPacket = _headerBuffer.FastConcat(_buffer);
            _recievedBytes += (ulong)fullPacket.Length;
            DeserializeRetry(fullPacket, Connection.RemoteEndPoint);
        }

        private byte[] _headerBuffer = new byte[4];

        private byte[] _buffer;

        private void ReadStream(IAsyncResult ar)
        {
            int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_headerBuffer, 0));
            _buffer = new byte[length];
            Stream.Read(_buffer, 0, length);
            byte[] fullPacket = _headerBuffer.FastConcat(_buffer);
            _recievedBytes += (ulong)fullPacket.Length;
            DeserializeRetry(fullPacket, Connection.RemoteEndPoint);
            _readResult = null;
            Stream.EndRead(ar);
        }
    }
}

#endif