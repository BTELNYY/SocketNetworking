using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem;

namespace SocketNetworking.Transports
{
    /// <summary>
    /// A generic class to represent a connection to a peer in a managed way.
    /// </summary>
    public abstract class NetworkTransport
    {
        public NetworkTransport()
        {
            Buffer = new byte[BufferSize];
        }

        /// <summary>
        /// Size of the buffer
        /// </summary>
        public int BufferSize { get; set; } = Packet.MaxPacketSize;

        /// <summary>
        /// Internal Buffer, will be modified
        /// </summary>
        public byte[] Buffer { get; protected set; } = new byte[] { };

        /// <summary>
        /// Is there anything to read from the Network interface?
        /// </summary>
        public abstract bool DataAvailable { get; }

        /// <summary>
        /// How much data is there to read from the network interface?
        /// </summary>
        public abstract int DataAmountAvailable { get; }

        /// <summary>
        /// Forcibly overwrites the <see cref="Buffer"/>
        /// </summary>
        public void FlushBuffer()
        {
            Buffer = new byte[BufferSize];
        }

        /// <summary>
        /// What <see cref="IPEndPoint"/> am I connected to?
        /// </summary>
        public abstract IPEndPoint Peer { get; }

        /// <summary>
        /// The local <see cref="IPEndPoint"/> of the current <see cref="Socket"/>
        /// </summary>
        public abstract IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// The <see cref="IPAddress"/> or <see cref="Peer"/>.
        /// </summary>
        public abstract IPAddress PeerAddress { get; }

        /// <summary>
        /// The port of the <see cref="Peer"/>
        /// </summary>
        public abstract int PeerPort { get; }

        /// <summary>
        /// Is the tranpsort currently connected?
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// The <see cref="System.Net.Sockets.Socket"/> responsible for data transfer
        /// </summary>
        public abstract Socket Socket { get; }

        /// <summary>
        /// Connects to a remote host (No DNS lookup is done)
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public abstract Exception Connect(string hostname, int port);

        /// <summary>
        /// Send data to a specific host.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public abstract Exception Send(byte[] data, IPEndPoint destination);

        /// <summary>
        /// Does the same as <see cref="SendAsync(byte[], IPEndPoint)"/>, but is async.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public abstract Task<Exception> SendAsync(byte[] data, IPEndPoint destination);

        /// <summary>
        /// Send data to the <see cref="Peer"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract Exception Send(byte[] data);

        /// <summary>
        /// Does the exact same as <see cref="Send(byte[])"/> but is async.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract Task<Exception> SendAsync(byte[] data);

        /// <summary>
        /// Recieve Data, this is a blocking task. See <see cref="DataAvailable"/> to implement a non-blocking approach.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public abstract (byte[], Exception, IPEndPoint) Receive();

        /// <summary>
        /// Does the same as <see cref="Receive"/>, but is async.
        /// </summary>
        /// <returns></returns>
        public abstract Task<(byte[], Exception, IPEndPoint)> ReceiveAsync();

        /// <summary>
        /// Closes the transport.
        /// </summary>
        public abstract void Close();
    }
}
