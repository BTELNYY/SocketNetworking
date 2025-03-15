using SocketNetworking.Shared.Serialization;
using System;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class KeepAlivePacket : Packet
    {
        public override PacketType Type => PacketType.KeepAlive;

        public long ReceivedTime { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            ReceivedTime = reader.ReadLong();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteLong(ReceivedTime);
            return writer;
        }
    }
}
