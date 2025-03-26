using System.Collections.Generic;
using SocketNetworking.Shared.PacketSystem.TypeWrappers;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class PacketMappingPacket : Packet
    {
        public override PacketType Type => PacketType.PacketMapping;

        public Dictionary<int, string> Mapping { get; set; } = new Dictionary<int, string>();

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Mapping = reader.ReadPacketSerialized<SerializableDictionary<int, string>>().ContainedDict;
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WritePacketSerialized<SerializableDictionary<int, string>>(new SerializableDictionary<int, string>(Mapping));
            return writer;
        }
    }
}
