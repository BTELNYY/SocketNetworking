using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.UnityEngine.Packets
{
    [PacketDefinition]
    public class NetworkObjectDestroyPacket : CustomPacket
    {
        public int DestroyID { get; set; } = 0;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(DestroyID);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            DestroyID = reader.ReadInt();
            return reader;
        }
    }
}
