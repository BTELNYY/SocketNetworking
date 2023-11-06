using SocketNetworking.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem
{
    public class Packet
    {
        public const int MaxPacketSize = 8192;

        public static readonly Type[] SupportedTypes =
        {
            typeof(string),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(IPacketSerializable),
        };

        public static byte[] SerializeSupportedType(object value)
        {
            if(!SupportedTypes.Contains(value.GetType())) 
            {
                throw new ArgumentException("Can't serialize an unsupported type.", "value");
            }
            if(value is IPacketSerializable serializable) { return serializable.Serialize(); }
            if (value is long l) { return BitConverter.GetBytes(l); }
            if (value is int i) { return BitConverter.GetBytes(i); }
            if (value is short s) { return BitConverter.GetBytes(s); }
            if (value is ulong ul ) { return BitConverter.GetBytes(ul); }
            if (value is uint ui ) { return BitConverter.GetBytes(ui); }
            if (value is ushort us ) { return BitConverter.GetBytes(us); }
            if (value is float f ) { return BitConverter.GetBytes(f); }
            if (value is double d ) { return BitConverter.GetBytes(d); }
            if (value is bool b) { return BitConverter.GetBytes(b); }
            if (value is byte by) { return BitConverter.GetBytes(by); }
            if (value is sbyte sby) { return BitConverter.GetBytes(sby); }
            if (value is string st)
            {
                PacketWriter writer = new PacketWriter();
                writer.WriteString(st);
                return writer.Data;
            }
            throw new NotImplementedException("No check caught type provided.");
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
            PacketReader reader = new PacketReader(data);
            int size = reader.ReadInt();
            PacketType type = (PacketType)reader.ReadInt();
            int networkTarget = reader.ReadInt();
            int customPacketID = 0;
            if(type == PacketType.CustomPacket)
            {
                customPacketID = reader.ReadInt();
            }
            return new PacketHeader(type, networkTarget, customPacketID, size);
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
        /// The NetworkID of the object which this packet is being sent to. -1 Means all objects.
        /// </summary>
        public int NetowrkIDTarget  = -1;

        /// <summary>
        /// Function called to serialize packets. Ensure you get the return type of this function becuase you'll need to add on to that array. creating a new array will cause issues.
        /// </summary>
        /// <returns>
        /// A <see cref="PacketWriter"/> which represents the packet being written, it is suggested you use this as your writer when creating subclasses.
        /// </returns>
        public virtual PacketWriter Serialize()
        {
            PacketWriter writer = new PacketWriter();
            writer.WriteInt((int)Type);
            writer.WriteInt(NetowrkIDTarget);
            return writer;
        }

        /// <summary>
        /// Deserialize the packet, always call the base function at the top of your override. Do not create new PacketReaders, use the one provided instead.
        /// </summary>
        /// <returns>
        /// The current <see cref="PacketReader"/> instance
        /// </returns>
        public virtual PacketReader Deserialize(byte[] data)
        {
            PacketReader reader = new PacketReader(data);
            //Very cursed, must read first in so that the desil doesn't fail next line.
            Size = reader.ReadInt();
            PacketType type = (PacketType)reader.ReadInt();
            if(type != Type)
            {
                throw new InvalidNetworkDataException("Given network data doesn't match packets internal data type. Either routing failed, or deserialization failed.");
            }
            NetowrkIDTarget = reader.ReadInt();
            return reader;
        }
    }

    /// <summary>
    /// Structure which represents what kind of packet is being sent, Note that the only type the user should use is CustomPacket.
    /// </summary>
    public enum PacketType 
    {
        None,
        ReadyStateUpdate,
        ConnectionStateUpdate,
        ClientData,
        ServerData,
        CustomPacket,
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
    /// Represents the first fields of a packet in a nicer way, to get this from a raw <see cref="byte[]"/> use <see cref="Packet.ReadPacketHeader(byte[])"/>
    /// </summary>
    public struct PacketHeader
    {
        public int Size;
        public PacketType Type;
        public int NetworkIDTarget;
        public int CustomPacketID;

        public PacketHeader(PacketType type, int networkIDTarget, int customPacketID, int size)
        {
            Type = type;
            NetworkIDTarget = networkIDTarget;
            CustomPacketID = customPacketID;
            Size = size;
        }

        public PacketHeader(PacketType type, int networkIDTarget, int size)
        {
            Type = type;
            NetworkIDTarget = networkIDTarget;
            CustomPacketID = 0;
            Size = size;
        }
    }
}
