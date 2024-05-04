using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;

namespace SocketNetworking.UnityEngine.Packets
{
    [PacketDefinition]
    public class NetworkObjectEnabledStatusPacket : CustomPacket
    {
        public bool IsEnabled { get; set; } = true;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteBool(IsEnabled);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            IsEnabled = reader.ReadBool();
            return reader;
        }
    }
}
