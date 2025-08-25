using System;
using SocketNetworking.Client;
using SocketNetworking.Shared.NetworkObjects;

namespace SocketNetworking.Shared
{

    /// <summary>
    /// Determines the <see cref="OwnershipMode"/> of <see cref="INetworkObject"/>s. See <see cref="INetworkObject.OwnershipMode"/>. This determines if certain actions can be made, such as running Network Invoke methods or changing <see cref="SyncVars.INetworkSyncVar"/>s.
    /// </summary>
    public enum OwnershipMode : byte
    {
        /// <summary>
        /// The client owns the object.
        /// </summary>
        Client,
        /// <summary>
        /// The server owns the object.
        /// </summary>
        Server,
        /// <summary>
        /// Everyone owns the object.
        /// </summary>
        Public
    }

    /// <summary>
    /// Structure which represents what kind of packet is being sent. The only <see cref="PacketType"/> that is important to developers using this library is <see cref="Custom"/>.
    /// </summary>
    public enum PacketType : byte
    {
        None,
        ReadyStateUpdate,
        AuthenticationStateUpdate,
        ConnectionStateUpdate,
        ClientData,
        PacketMapping,
        ServerData,
        NetworkInvocation,
        NetworkInvocationResult,
        Encryption,
        SyncVarUpdate,
        ObjectManage,
        KeepAlive,
        SSLUpgrade,
        Stream,
        ClientToClient,
        Authentication,
        Custom,
    }

    /// <summary>
    /// <see cref="PacketFlags"/> are used to give metadata to packets for the sending method to interpert. For example, flagging the packet as <see cref="PacketFlags.SymmetricalEncrypted"/> will use the symmetrical key sent in the Encryption Handshake during connection time. Some flags cannot be used if the framework for them has not yet been implemented. (Handshake isn't complete, RSA/AES keys are not exchanged, or they are incorrect.)
    /// </summary>
    [Flags]
    public enum PacketFlags : short
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
        /// Uses the RSA Algorithm at send to encrypt data. RSA has a limit to the size of the data it can encrypt. Not Compatible with <see cref="PacketFlags.SymmetricalEncrypted"/>
        /// </summary>
        AsymmetricalEncrypted = 2,
        /// <summary>
        /// Uses Symmetrical Encryption to send data, note that this can only be used once the full encryptoin handshake has been completed. Not Compatible with <see cref="PacketFlags.AsymmetricalEncrypted"/>
        /// </summary>
        SymmetricalEncrypted = 4,
        /// <summary>
        /// Used to specify this packet is to be treated with priority, being sent first. This can cause out of order situations, so its best to use this only for systems which are not affected by this. Does nothing if you are not using <see cref="MixedNetworkClient"/>. If you want to try keeping this packets in order, flip <see cref="PacketFlags.KeepInOrder"/> to true.
        /// </summary>
        Priority = 8,
        /// <summary>
        /// Tells the library to not Encrypt this packet at all, if combined with <see cref="PacketFlags.AsymmetricalEncrypted"/> or <see cref="PacketFlags.SymmetricalEncrypted"/>, the encryption flags are ignored.
        /// </summary>
        DoNotEncrypt = 16,
        /// <summary>
        /// Prevents this packet from being encrypted using <see cref="PacketFlags.AsymmetricalEncrypted"/>.
        /// </summary>
        NoRSA = 32,
        /// <summary>
        /// Prevents this packet from being encrypted using <see cref="PacketFlags.SymmetricalEncrypted"/>.
        /// </summary>
        NoAES = 64,
        /// <summary>
        /// Prevents this packet from being sent out of order. Does nothing if you do not have <see cref="PacketFlags.Priority"/> set to true or are not using <see cref="MixedNetworkClient"/>. This is done by checking the <see cref="PacketSystem.Packet.SendTime"/> of this packet compared to the last recieved packet. The last recieved packet is defined as the last packet of the same <see cref="PacketSystem.Packet.Type"/>, <see cref="PacketSystem.Packet.Flags"/> and <see cref="PacketSystem.TargetedPacket.NetworkIDTarget"/> (if applicable, otherwise uses size). Note that the old packet must also have this flag set in order to count, packets without this flag are simply not checked.
        /// </summary>
        KeepInOrder = 128,
        /// <summary>
        /// Determines if the current packet is <see cref="PacketSystem.TargetedPacket"/>/
        /// </summary>
        IsTargeted = 256,
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
    /// Determines where a <see cref="INetworkObject"/> can be "seen" from, or be visible on the network
    /// </summary>
    public enum ObjectVisibilityMode : byte
    {
        /// <summary>
        /// Only the server has this object. Clients do not receive information about it. (Spawning a <see cref="INetworkObject"/> will set this to <see cref="Everyone"/>
        /// </summary>
        ServerOnly,
        /// <summary>
        /// Only the <see cref="INetworkObject.OwnerClientID"/> and the Server can see this object.
        /// </summary>
        OwnerAndServer,
        /// <summary>
        /// Everyone can see this object.
        /// </summary>
        Everyone,
    }

    /// <summary>
    /// Mode of the network invocation. <see cref="Listener"/> is for packet listenrs, <see cref="RemoteProcedureCall"/> is for Network Invocations.
    /// </summary>
    public enum InvocationMode
    {
        InternalCall,
        Listener,
        RemoteProcedureCall,
    }
}
