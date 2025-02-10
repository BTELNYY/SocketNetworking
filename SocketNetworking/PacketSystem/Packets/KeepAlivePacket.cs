using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class KeepAlivePacket : Packet
    {
        public override PacketType Type => PacketType.KeepAlive;

        public ulong SentTime { get; set; } = 0;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteULong(SentTime);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            SentTime = reader.ReadULong();
            return reader;
        }
    }
}
