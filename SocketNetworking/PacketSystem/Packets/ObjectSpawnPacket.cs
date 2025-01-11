using SocketNetworking.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class ObjectSpawnPacket : Packet
    {
        public override PacketType Type => PacketType.ObjectSpawn;

        public string ObjectClassName { get; set; } = "";

        public string AssmeblyName { get; set; } = "";

        public byte[] ExtraData { get; set; } = new byte[0];

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteString(ObjectClassName);
            writer.WriteString(AssmeblyName);
            writer.WriteByteArray(ExtraData);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            ObjectClassName = reader.ReadString();
            AssmeblyName = reader.ReadString();
            ExtraData = reader.ReadByteArray();
            return reader;
        }
    }
}
