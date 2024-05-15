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

        public int OwnerID { get; set; } = -1;

        public OwnershipMode OwnershipMode { get; set; } = OwnershipMode.Server;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(PrefabID);
            writer.WriteInt(NewNetworkID);
            writer.WriteInt(OwnerID);
            writer.WriteByte((byte)OwnershipMode);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            PrefabID = reader.ReadInt();
            NewNetworkID = reader.ReadInt();
            OwnerID = reader.ReadInt();
            OwnershipMode = (OwnershipMode)reader.ReadByte();
            return reader;
        }
    }
}
