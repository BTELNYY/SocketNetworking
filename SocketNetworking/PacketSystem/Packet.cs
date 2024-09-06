using SocketNetworking.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem
{
    public class Packet
    {
        public static int MaxPacketSize
        {
            get
            {
                return _maxPacketSize;
            }
        }

        private static readonly int _maxPacketSize = ushort.MaxValue;

        public static byte[] SerializeSupportedType(object value)
        {
            return NetworkConvert.Serialize(value).Data;
        }

        /// <summary>
        /// Read the first values of a packet to determine what kind it is.
        /// </summary>
        /// <param name="data">
        /// Raw <see cref="byte[]"/> of network data.
        /// </param>
        /// <returns>
        /// An instance of the <see cref="PacketHeader"/> structure with all fields filled in.
        /// </returns>
        public static PacketHeader ReadPacketHeader(byte[] data)
        {
            return PacketHeader.GetHeader(data);
        }

        /// <summary>
        /// This is unreliable. Do not use this to calculate any size.
        /// </summary>
        public int Size = 0;

        /// <summary>
        /// PacketType used to determine if the library should look for Listeners or handle internally.
        /// </summary>
        public virtual PacketType Type { get; } = PacketType.None;

        /// <summary>
        /// <see cref="PacketFlags"/> are used to determine metadata about the packet, such as encryption status or compression. Default value is <see cref="PacketFlags.None"/>. Setting this value on the sender applies the flags in transit, setting these flags otherwise causes no effects.
        /// </summary>
        public virtual PacketFlags Flags { get; set; } = PacketFlags.None;

        /// <summary>
        /// Method ensures that the <see cref="PacketFlags"/> set in <see cref="Flags"/> are not conflicting. 
        /// </summary>
        /// <returns>
        /// Either <see cref="true"/> for a successful validation or <see cref="false"/> for a failed one.
        /// </returns>
        public bool ValidateFlags()
        {
            if(Flags.HasFlag(PacketFlags.AsymtreicalEncrypted) && Flags.HasFlag(PacketFlags.SymetricalEncrypted))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// The NetworkID of the object which this packet is being sent to. 0 Means only sent to the other clients class.
        /// </summary>
        public int NetowrkIDTarget = 0;

        /// <summary>
        /// This will only be a value greater then 0 if <see cref="Type"/> is <see cref="PacketType.CustomPacket"/>
        /// </summary>
        public virtual int CustomPacketID { get; private set; } = -1;

        /// <summary>
        /// Function called to serialize packets. Ensure you get the return type of this function becuase you'll need to add on to that array. creating a new array will cause issues.
        /// </summary>
        /// <returns>
        /// A <see cref="ByteWriter"/> which represents the packet being written, it is suggested you use this as your writer when creating subclasses.
        /// </returns>
        public virtual ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteByte((byte)Type);
            writer.WriteByte((byte)Flags);
            writer.WriteInt(NetowrkIDTarget);
            writer.WriteInt(CustomPacketID);
            return writer;
        }

        /// <summary>
        /// Deserialize the packet, always call the base function at the top of your override. Do not create new PacketReaders, use the one provided instead.
        /// </summary>
        /// <returns>
        /// The current <see cref="ByteReader"/> instance
        /// </returns>
        public virtual ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            //Very cursed, must read first in so that the desil doesn't fail next line.
            int expectedLength = reader.DataLength - PacketHeader.HeaderLength;
            Size = reader.ReadInt();
            PacketType type = (PacketType)reader.ReadByte();
            if(type != Type)
            {
                throw new InvalidNetworkDataException("Given network data doesn't match packets internal data type. Either routing failed, or deserialization failed.");
            }
            PacketFlags flags = (PacketFlags)reader.ReadByte();
            Flags = flags;
            NetowrkIDTarget = reader.ReadInt();
            CustomPacketID = reader.ReadInt();
            if(expectedLength != reader.DataLength)
            {
                throw new InvalidNetworkDataException("Packet Deserializer stole more bytes then it should!");
            }
            return reader;
        }
    }

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
        Compressed,
        /// <summary>
        /// Uses the RSA Algorithim at send to encrypt data. RSA has a limit to the size of the data it can encrypt. Not Compatible with <see cref="PacketFlags.SymetricalEncrypted"/>
        /// </summary>
        AsymtreicalEncrypted,
        /// <summary>
        /// Uses Symetrical Encryption to send data, note that this can only be used once the full encryptoin handshake has been completed. Not Compatible with <see cref="PacketFlags.AsymtreicalEncrypted"/>
        /// </summary>
        SymetricalEncrypted,
    }

    /// <summary>
    /// Represents the first fields of a packet in a nicer way, to get this from a raw <see cref="byte[]"/> use <see cref="Packet.ReadPacketHeader(byte[])"/>
    /// </summary>
    public struct PacketHeader
    {
        public const int HeaderLength = 14;

        public int Size;
        public PacketType Type;
        public PacketFlags Flags;
        public int NetworkIDTarget;
        public int CustomPacketID;

        public PacketHeader(PacketType type, int networkIDTarget, int customPacketID, int size)
        {
            Type = type;
            NetworkIDTarget = networkIDTarget;
            CustomPacketID = customPacketID;
            Size = size;
            Flags = PacketFlags.None;
        }

        public PacketHeader(PacketType type, int networkIDTarget, int size)
        {
            Type = type;
            NetworkIDTarget = networkIDTarget;
            CustomPacketID = 0;
            Size = size;
            Flags = PacketFlags.None;
        }

        public PacketHeader(int size, PacketType type, PacketFlags flags, int networkIDTarget, int customPacketID)
        {
            Size = size;
            Type = type;
            Flags = flags;
            NetworkIDTarget = networkIDTarget;
            CustomPacketID = customPacketID;
        }

        /// <summary>
        /// Gets a <see cref="PacketHeader"/> using at minimum 16 bytes of data.
        /// </summary>
        /// <param name="data">
        /// A 16 <see cref="byte"/> long array to use to fetch the packet header.
        /// </param>
        /// <returns>
        /// The completed <see cref="PacketHeader"/>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static PacketHeader GetHeader(byte[] data)
        {
            if(data.Length < HeaderLength + 1)
            {
                throw new ArgumentOutOfRangeException("data", "Data must be at least 17 bytes long!");
            }
            ByteReader reader = new ByteReader(data);
            int size = reader.ReadInt();
            PacketType type = (PacketType)reader.ReadByte();
            PacketFlags flags = (PacketFlags)reader.ReadByte();
            int networkTarget = reader.ReadInt();
            int customPacketID = reader.ReadInt();
            return new PacketHeader(size, type, flags, networkTarget, customPacketID);
        }
    }
}
