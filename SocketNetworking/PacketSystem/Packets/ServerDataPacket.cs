using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;

namespace SocketNetworking.PacketSystem.Packets
{
    [PacketDefinition]
    public sealed class ServerDataPacket : Packet
    {
        public sealed override PacketType Type => PacketType.ServerData;

        public int YourClientID { get; private set; } = 0;

        public ProtocolConfiguration Configuration { get; private set; } = new ProtocolConfiguration();

        public override PacketWriter Serialize()
        {
            PacketWriter writer = base.Serialize();
            writer.WriteInt(YourClientID);
            writer.Write<ProtocolConfiguration>(Configuration);
            return writer;
        }

        public override PacketReader Deserialize(byte[] data)
        {
            PacketReader reader = base.Deserialize(data);
            YourClientID = reader.ReadInt();
            Configuration = reader.Read<ProtocolConfiguration>();
            return reader;
        }
    }
}
