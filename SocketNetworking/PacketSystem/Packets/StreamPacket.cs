using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class StreamPacket : Packet
    {
        public override PacketType Type => PacketType.StreamPacket;

        public StreamFunction Function { get; set; }

        public ushort StreamID { get; set; }

        public bool Error { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

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
            return reader;
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
    }
}
