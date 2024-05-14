using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem.TypeWrappers;
using SocketNetworking.Misc;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class ServerDataPacket : Packet
    {
        public sealed override PacketType Type => PacketType.ServerData;

        public int YourClientID = 0;

        public ProtocolConfiguration Configuration = new ProtocolConfiguration();

        public Dictionary<int, string> CustomPacketAutoPairs = new Dictionary<int, string>();

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(YourClientID);
            writer.Write<ProtocolConfiguration>(Configuration);
            SerializableDictionary<int, string> dict = new SerializableDictionary<int, string>(CustomPacketAutoPairs);
            writer.Write<SerializableDictionary<string, int>>(dict);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            YourClientID = reader.ReadInt();
            Configuration = reader.Read<ProtocolConfiguration>();
            CustomPacketAutoPairs = reader.Read<SerializableDictionary<int, string>>().ContainedDict;
            return reader;
        }
    }
}
