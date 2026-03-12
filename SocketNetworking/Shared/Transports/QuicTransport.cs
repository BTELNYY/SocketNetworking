using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SocketNetworking.Client;


#if NET8_0_OR_GREATER
using System.Net.Quic;

namespace SocketNetworking.Shared.Transports
{
    [RequiresPreviewFeatures]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macOS")]
    public class QuicTransport : NetworkTransport
    {
        public override bool DataAvailable => true;

        public override int DataAmountAvailable => 4;

        public override IPEndPoint Peer => Connection.RemoteEndPoint;

        public override IPEndPoint LocalEndPoint => Connection.LocalEndPoint;

        public override IPAddress PeerAddress => Peer.Address;

        public override int PeerPort => Peer.Port;

        public override bool IsConnected => Stream.CanRead && Stream.CanWrite;

        public override Socket Socket => throw new InvalidOperationException("Quic Clients don't use sockets.");

        public QuicConnection Connection { get; set; }

        public QuicStream Stream { get; set; }

        public QuicNetworkClient QuicClient => Client as QuicNetworkClient;

        public override void Close()
        {
            CloseAsync().Wait();
        }

        public override async Task CloseAsync()
        {
            if (Connection == null)
            {
                return;
            }
            await Connection.CloseAsync(QuicNetworkClient.DefaultStreamClosedCode);
        }

        public override Exception Connect(string hostname, int port)
        {
            return ConnectAsync(hostname, port).Result;
        }

        public event Func<bool, object, X509Certificate, X509Chain, SslPolicyErrors, bool> RemoteCertValidationCallback;

        protected bool RemoteCertificateValidationCallback(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
        {
            //TODO: Implement proper SSL cert checks.
            bool result = false;
            if (errors != SslPolicyErrors.None)
            {
                QuicClient.Log.Error($"SSL Policy Error! {errors}");
                result = false;
            }
            else
            {
                result = true;
            }
            if (RemoteCertValidationCallback != null)
            {
                result = RemoteCertValidationCallback(result, sender, cert, chain, errors);
            }
            return result;
        }

        public override async Task<Exception> ConnectAsync(string hostname, int port)
        {
            QuicClientConnectionOptions clientConnectionOptions = new QuicClientConnectionOptions()
            {
                RemoteEndPoint = IPEndPoint.Parse(hostname + ":" + port),
                DefaultCloseErrorCode = QuicNetworkClient.DefaultErrorCode,
                DefaultStreamErrorCode = QuicNetworkClient.DefaultStreamClosedCode,
                ClientAuthenticationOptions = new System.Net.Security.SslClientAuthenticationOptions()
                {
                    ApplicationProtocols =
                    [
                        new SslApplicationProtocol(QuicClient.ClientConfiguration.Protocol)
                    ],
                    TargetHost = hostname,
                    RemoteCertificateValidationCallback = RemoteCertificateValidationCallback
                }
            };
            clientConnectionOptions = QuicClient.InvokePreConnect(clientConnectionOptions);
            try
            {
                QuicConnection connection = await QuicConnection.ConnectAsync(clientConnectionOptions);
                QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                Connection = connection;
                Stream = stream;
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        public override (byte[], Exception, IPEndPoint) Receive()
        {
            return ReceiveAsync().Result;
        }

        public override async Task<(byte[], Exception, IPEndPoint)> ReceiveAsync()
        {
            try
            {
                byte[] _headerBuffer = new byte[sizeof(int)];
                await Stream.ReadExactlyAsync(_headerBuffer.AsMemory(0, sizeof(int)));
                int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_headerBuffer, 0));
                byte[] _buffer = new byte[length];
                await Stream.ReadExactlyAsync(_buffer.AsMemory(0, length));
                byte[] fullPacket = _headerBuffer.FastConcat(_buffer);
                return (fullPacket, null, Peer);
            }
            catch (Exception ex)
            {
                return (null, ex, Peer);
            }
        }

        public override Exception Send(byte[] data, IPEndPoint destination)
        {
            if (destination == Peer)
            {
                return Send(data);
            }
            throw new InvalidOperationException("Can't send to arbitrary hosts.");
        }

        public override Exception Send(byte[] data)
        {
            return SendAsync(data).Result;
        }

        public override async Task<Exception> SendAsync(byte[] data, IPEndPoint destination)
        {
            if (destination == Peer)
            {
                return await SendAsync(data);
            }
            throw new InvalidOperationException("Can't send to arbitrary hosts.");
        }

        public override async Task<Exception> SendAsync(byte[] data)
        {
            try
            {
                await Stream.WriteAsync(data);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
#endif