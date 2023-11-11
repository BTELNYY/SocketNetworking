using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class ServerDataPacket : Packet
    {
        public sealed override PacketType Type => PacketType.ServerData;

        public int YourClientID = 0;

        public ProtocolConfiguration Configuration  = new ProtocolConfiguration();

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(YourClientID);
            writer.Write<ProtocolConfiguration>(Configuration);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            YourClientID = reader.ReadInt();
            Configuration = reader.Read<ProtocolConfiguration>();
            return reader;
        }
    }
}
