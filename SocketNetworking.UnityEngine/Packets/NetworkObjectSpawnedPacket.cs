using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace SocketNetworking.UnityEngine.Packets
{
    [PacketDefinition]
    public class NetworkObjectSpawnedPacket : CustomPacket
    {
        public int SpawnedNetworkID { get; set; } = 0;

        public int SpawnedPrefabID { get; set; } = 0;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(SpawnedNetworkID);
            writer.WriteInt(SpawnedPrefabID);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            SpawnedNetworkID = reader.ReadInt();
            SpawnedPrefabID = reader.ReadInt();
            return reader;
        }
    }
}
