using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;

namespace SocketNetworking.PacketSystem.Packets
{
    [PacketDefinition]
    public sealed class ReadyStateUpdatePacket : Packet
    {
        public sealed override PacketType Type => PacketType.ReadyStateUpdate;

        public bool Ready = false;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteBool(Ready);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Ready = reader.ReadBool();
            return reader;
        }
    }
}
