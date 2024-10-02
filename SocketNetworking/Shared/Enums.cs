using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        CustomPacket,
    }

    /// <summary>
    /// <see cref="PacketFlags"/> are used to give metadata to packets for the sending method to interpert. For exmaple, flagging the packet as <see cref="PacketFlags.SymetricalEncrypted"/> will use the symmetrical key sent in the Encryption Handshake during connection time. Some flags cannot be used if the framework for them has not yet been implemented. (Handshake isn't complete, RSA/AES keys are not exchanged, or they are incorrect.)
    /// </summary>
    [Flags]
    public enum PacketFlags : byte
    {
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
    public enum PacketDirection
    {
        Client,
        Server,
        Any,
    }
}
