using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;

namespace SocketNetworking.Shared
{

    /// <summary>
    /// Structure which represents what kind of packet is being sent, Note that the only type the user should use is CustomPacket.
    /// </summary>
    public enum PacketType : byte
    {
        None,
        ReadyStateUpdate,
        ConnectionStateUpdate,
        ClientData,
        ServerData,
        NetworkInvocation,
        NetworkInvocationResult,
        EncryptionPacket,
        SyncVarUpdate,
        ObjectSpawn,
        CustomPacket,
    }

    /// <summary>
    /// <see cref="PacketFlags"/> are used to give metadata to packets for the sending method to interpert. For exmaple, flagging the packet as <see cref="PacketFlags.SymetricalEncrypted"/> will use the symmetrical key sent in the Encryption Handshake during connection time. Some flags cannot be used if the framework for them has not yet been implemented. (Handshake isn't complete, RSA/AES keys are not exchanged, or they are incorrect.)
    /// </summary>
    [Flags]
    public enum PacketFlags : byte
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Uses GZIP Compression.
        /// </summary>
        Compressed = 1,
        /// <summary>
        /// Uses the RSA Algorithim at send to encrypt data. RSA has a limit to the size of the data it can encrypt. Not Compatible with <see cref="PacketFlags.SymetricalEncrypted"/>
        /// </summary>
        AsymtreicalEncrypted = 2,
        /// <summary>
        /// Uses Symetrical Encryption to send data, note that this can only be used once the full encryptoin handshake has been completed. Not Compatible with <see cref="PacketFlags.AsymtreicalEncrypted"/>
        /// </summary>
        SymetricalEncrypted = 4,
        /// <summary>
        /// Used to specify this packet is to be treated with priority, being sent first. This can cause out of order situations, so its best to use this only for systems which are not affected by this.
        /// </summary>
        Priority = 8,
        /// <summary>
        /// Tells the library to not Encrypt this packet at all, if combined with <see cref="PacketFlags.AsymtreicalEncrypted"/> or <see cref="PacketFlags.SymetricalEncrypted"/>, the encryption flags are ignored.
        /// </summary>
        DoNotEncrypt = 16,
    }

    /// <summary>
    /// Represents the encryption state withe the remote client/server.
    /// </summary>
    public enum EncryptionState : byte
    {
        Disabled,
        Handshake,
        AsymmetricalReady,
        SymmetricalReady,
        Encrypted
    }

    /// <summary>
    /// The current state of the handshake. Disconnect = Either not connected at all or just got disconnected. Handshake = Client-Server still agreeing on protocol and version. Connected = System connected.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Handshake,
        Connected,
    }

    /// <summary>
    /// Location of the client. Local means on the local machine ("client") and remote means on the remote machine ("server"). Unknown is used when the connection isn't active or the location can't be determined.
    /// </summary>
    public enum ClientLocation
    {
        Local,
        Remote,
        Unknown
    }

    /// <summary>
    /// An enum which represents the direction from which the Packet was sent.
    /// </summary>
    public enum NetworkDirection
    {
        Client,
        Server,
        Any,
    }

    /// <summary>
    /// Defines the behavior of the <see cref="UdpNetworkClient"/>
    /// </summary>
    public enum UdpClientMode
    {
        /// <summary>
        /// Assumes the client is connecting to a server and nobody else.
        /// </summary>
        ServerClient,
        /// <summary>
        /// Assumes the client is connecting to a server, but may have non-authoritive peers
        /// </summary>
        Peer2PeerWithAuthoritivePeer,
        /// <summary>
        /// Assumes the client has no central server, only peers.
        /// </summary>
        Peer2Peer,
    }

    /// <summary>
    /// Mode of the network invocation. <see cref="Listener"/> is for packet listenrs, <see cref="RemoteProcedureCall"/> is for Network Invocations.
    /// </summary>
    public enum InvocationMode
    {
        Listener,
        RemoteProcedureCall
    }
}
