using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem.TypeWrappers;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class StreamPacket : Packet
    {
        public override PacketType Type => PacketType.StreamPacket;

        public StreamFunction Function { get; set; }

        public ushort StreamID { get; set; }

        public bool Error { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public Type StreamType { get; set; } = typeof(void);

        public byte[] Data { get; set; } = new byte[0];

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteByte((byte)Function);
            writer.WriteUShort(StreamID);
            writer.WriteBool(Error);
            if(Error)
            {
                writer.WriteString(ErrorMessage);
            }
            writer.WriteByteArray(Data);
            writer.WriteWrapper<SerializableType, Type>(new SerializableType(StreamType));
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Function = (StreamFunction)reader.ReadByte();
            StreamID = reader.ReadUShort();
            Error = reader.ReadBool();
            if(Error)
            {
                ErrorMessage = reader.ReadString();
            }
            Data = reader.ReadByteArray();
            StreamType = reader.ReadWrapper<SerializableType, Type>();
            return reader;
        }

        public override string ToString()
        {
            string s = base.ToString();
            s += $" Function: {Function}, ID: {StreamID}, Data Length: {Data.Length}";
            return s;
        }
    }

    public enum StreamFunction : byte
    {
        DataSend,
        DataRequest,
        Open,
        Close,
        Accept,
        Reject,
        MetaData,
        CustomData,
    }
}
