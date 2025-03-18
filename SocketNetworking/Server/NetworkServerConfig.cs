using System;
using System.Security.Cryptography.X509Certificates;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;

namespace SocketNetworking.Server
{
    public class NetworkServerConfig
    {
        /// <summary>
        /// The Servers <see cref="ServerEncryptionMode"/>.
        /// </summary>
        public ServerEncryptionMode EncryptionMode { get; set; } = ServerEncryptionMode.Required;

        /// <summary>
        /// Should the server accept the connection, then instantly disconnect the client with a message?
        /// </summary>
        public bool AutoDisconnectClients { get; set; } = false;

        /// <summary>
        /// Message to auto disconnect clients with
        /// </summary>
        public string AutoDisconnectMessage { get; set; } = "Server is not ready!";

        /// <summary>
        /// How long should the server wait for the client to complete the handshake?
        /// </summary>
        public float HandshakeTime { get; set; } = 10f;

        /// <summary>
        /// The server password, this will never be sent accross the network.
        /// </summary>
        public string ServerPassword { get; set; } = "default";

        /// <summary>
        /// Should the server check for client passwords?
        /// </summary>
        public bool UseServerPassword { get; set; } = false;

        /// <summary>
        /// If not an empty string, will be used to prove server identity for <see cref="TcpNetworkClient"/> and <see cref="MixedNetworkClient"/> clients. This should be a path to the certificate.
        /// </summary>
        public string CertificatePath { get; set; } = "";

        /// <summary>
        /// The loaded certificate.
        /// </summary>
        public X509Certificate Certificate
        {
            get
            {
                return _cert;
            }
            set
            {
                _cert = value;
            }
        }

        [NonSerialized]
        private X509Certificate _cert;

        /// <summary>
        /// What port should the server start on?
        /// </summary>
        public int Port { get; set; } = 7777;

        /// <summary>
        /// What IP should the server bind to?
        /// </summary>
        public string BindIP { get; set; } = "0.0.0.0";

        /// <summary>
        /// Should the server Auto-Ready Clients when the the <see cref="NetworkClient.CurrentConnectionState"/> becomes <see cref="ConnectionState.Connected"/>?
        /// </summary>
        public bool DefaultReady { get; set; } = true;

        /// <summary>
        /// When true, will automatically begin syncing <see cref="INetworkObject"/>s by spawning them in. This will also call <see cref="INetworkObject.OnSync(NetworkClient)"/> on the objects before the spawn packet is sent.
        /// </summary>
        public bool AutoSync { get; set; } = true;

        /// <summary>
        /// How many threads should the server spawn to handle clients? (Formula is, <see cref="MaximumClients"/> divided by <see cref="DefaultThreads"/> to see your Client to Thread ratio)
        /// </summary>
        public int DefaultThreads { get; set; } = 4;

        /// <summary>
        /// Amount of concurrent connections total, anyone else will be ignored.
        /// </summary>
        public int MaximumClients { get; set; } = 100;

        public int ClientsPerThread
        {
            get
            {
                return MaximumClients / DefaultThreads;
            }
        }
    }
}
