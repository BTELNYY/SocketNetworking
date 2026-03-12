using System.Collections.Generic;
using SocketNetworking.Shared.Messages;
using SocketNetworking.Shared.PacketSystem.TypeWrappers;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class ClientDataPacket : Packet
    {
        public sealed override PacketType Type => PacketType.ClientData;

        public ProtocolConfiguration Configuration { get; set; } = new ProtocolConfiguration();

        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Configuration = reader.ReadPacketSerialized<ProtocolConfiguration>();
            Headers = reader.ReadPacketSerialized<SerializableDictionary<string, string>>().ContainedDict;
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WritePacketSerialized<ProtocolConfiguration>(Configuration);
            writer.WritePacketSerialized<SerializableDictionary<string, string>>(new SerializableDictionary<string, string>(Headers));
            return writer;
        }
    }
}
