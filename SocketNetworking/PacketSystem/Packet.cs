using SocketNetworking.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;
using System.Net;
using SocketNetworking.PacketSystem.TypeWrappers;

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
        public virtual PacketFlags Flags 
        { 
            get; 
            set;
        } = PacketFlags.None;

        /// <summary>
        /// Method ensures that the <see cref="PacketFlags"/> set in <see cref="Flags"/> are not conflicting. 
        /// </summary>
        /// <returns>
        /// Either <see cref="true"/> for a successful validation or <see cref="false"/> for a failed one.
        /// </returns>
        public bool ValidateFlags()
        {
            if(Flags.HasFlag(PacketFlags.AsymetricalEncrypted) && Flags.HasFlag(PacketFlags.SymetricalEncrypted))
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
        /// The Destination of the packet.
        /// </summary>
        public IPEndPoint Destination { get; set; } = new IPEndPoint(IPAddress.Loopback, 0);

        /// <summary>
        /// The source of the packet. This isn't trusted on the recieving client, so it will be overwritten.
        /// </summary>
        public IPEndPoint Source { get; set; } = new IPEndPoint(IPAddress.Loopback, 0);

        /// <summary>
        /// This method will validate the packet and modify it as needed. For example, it will change flags if need be, or the content if it isn't properly defined.
        /// </summary>
        /// <returns>
        /// true if the validation suceeds, false otherwise.
        /// </returns>
        public virtual bool ValidatePacket()
        {
            return true;
        }

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
            SerializableIPEndPoint destination = new SerializableIPEndPoint(Destination);
            SerializableIPEndPoint source = new SerializableIPEndPoint(Source);
            writer.WriteWrapper<SerializableIPEndPoint, IPEndPoint>(destination);
            writer.WriteWrapper<SerializableIPEndPoint, IPEndPoint>(source);
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
            Flags = (PacketFlags)reader.ReadByte();
            NetowrkIDTarget = reader.ReadInt();
            CustomPacketID = reader.ReadInt();
            Destination = reader.ReadWrapper<SerializableIPEndPoint, IPEndPoint>();
            Source = reader.ReadWrapper<SerializableIPEndPoint, IPEndPoint>();
            //if (expectedLength != reader.DataLength)
            //{
            //    throw new InvalidNetworkDataException("Packet Deserializer stole more bytes then it should!");
            //}
            return reader;
        }
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
            if(data == null)
            {
                return new PacketHeader();
            }
            if(data.Length < HeaderLength)
            {
                throw new ArgumentOutOfRangeException("data", $"Data must be at least {HeaderLength} bytes long!");
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
