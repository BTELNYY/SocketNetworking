using System;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using SocketNetworking.Shared;

#if NET8_0_OR_GREATER
using System.Net.Quic;
using SocketNetworking.Shared.Transports;

namespace SocketNetworking.Client
{
    [RequiresPreviewFeatures]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macOS")]
    public class QuicNetworkClient : NetworkClient
    {
        public QuicNetworkClient() : base()
        {
            Transport = new QuicTransport();
        }

        public QuicTransport QuicTransport => Transport as QuicTransport;

        public const long DefaultErrorCode = 0x0A;

        public const long DefaultStreamClosedCode = 0x0B;

        public QuicConnection Connection => QuicTransport.Connection;

        public QuicStream Stream => QuicTransport.Stream;

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
                return QuicTransport.IsConnected;
            }
            set
            {
                base.IsConnected = value;
            }
        }

        public override IPEndPoint ConnectedPeer => Connection.RemoteEndPoint;

        public override bool IsTransportConnected => IsConnected;

        /// <summary>
        /// Called right before a connection is made in <see cref="ConnectAsync(string, ushort)"/>, used to allow the developer to modify the connection options before the connection is made.
        /// </summary>
        public event Func<QuicClientConnectionOptions, QuicClientConnectionOptions> OnPreConnect;

        public QuicClientConnectionOptions InvokePreConnect(QuicClientConnectionOptions opts)
        {
            if (OnPreConnect != null && OnPreConnect.GetInvocationList().Length > 0)
            {
                return OnPreConnect(opts);
            }
            return opts;
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
            Exception ex1 = await Transport.ConnectAsync(finalHostname, port);
            if (ex1 != null)
            {
                Log.Error($"Failed to connect: {ex1}");
                return false;
            }
            StartClient();
            return true;
        }
    }
}

#endif