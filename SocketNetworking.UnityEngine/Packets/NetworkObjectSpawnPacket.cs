using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.UnityEngine.Packets
{
    [PacketDefinition]
    public class NetworkObjectSpawnPacket : CustomPacket
    {
        public int PrefabID { get; set; } = 0;

        public int NewNetworkID { get; set; } = 0;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(PrefabID);
            writer.WriteInt(NewNetworkID);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            PrefabID = reader.ReadInt();
            NewNetworkID = reader.ReadInt();
            return reader;
        }
    }
}
