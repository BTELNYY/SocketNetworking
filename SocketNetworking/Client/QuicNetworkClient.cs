#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;

namespace SocketNetworking.Client
{
    [RequiresPreviewFeatures]
    public class QuicNetworkClient : NetworkClient
    {
        public const long DefaultErrorCode = 0x0A;

        public const long DefaultStreamClosedCode = 0x0B;

        public QuicConnection Connection { get; private set; }

        public QuicStream Stream { get; private set; }

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
        }

        public override bool Connect(string hostname, ushort port)
        {
            Task<bool> result = ConnectAsync(hostname, port);
            result.Wait();
            return result.Result;
        }

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
            QuicClientConnectionOptions options = new QuicClientConnectionOptions()
            {
                RemoteEndPoint = IPEndPoint.Parse(finalHostname + ":" + port),
                DefaultCloseErrorCode = DefaultErrorCode,
                DefaultStreamErrorCode = DefaultStreamClosedCode
            };
            QuicConnection connection = await QuicConnection.ConnectAsync(options);
            QuicStream stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
            Connection = connection;
            Stream = stream;
            return true;
        }

        public override void Disconnect(string message)
        {
            DisconnectAsync(message).Wait();
        }

        public async Task DisconnectAsync(string message)
        {
            base.Disconnect(message);
            await Connection.CloseAsync(0x0);
            return;
        }

        protected override void SendNextPacketInternal()
        {
            base.SendNextPacketInternal();
        }

        protected override void RawReader()
        {
            base.RawReader();
        }
    }
}

#endif