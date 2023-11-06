using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.Packets
{
    /// <summary>
    /// Base class for all custom packets, it is the only class accepted by library. Your CustomPacketID value must be unique per class.
    /// </summary>
    public class CustomPacket : Packet
    {
        public sealed override PacketType Type => PacketType.CustomPacket;

        public override PacketWriter Serialize()
        {
            PacketWriter writer = base.Serialize();
            return writer;
        }

        public override PacketReader Deserialize(byte[] data)
        {
            PacketReader reader = base.Deserialize(data);
            return reader;
        }
    }
}
