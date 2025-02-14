using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem.TypeWrappers;
using SocketNetworking.Shared;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class SyncVarUpdatePacket : Packet
    {
        public override PacketType Type => PacketType.SyncVarUpdate;

        public List<SyncVarData> Data { get; set; } = new List<SyncVarData>();

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            SerializableList<SyncVarData> data = new SerializableList<SyncVarData>(Data);
            writer.WritePacketSerialized<SerializableList<SyncVarData>>(data);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            SerializableList<SyncVarData> syncVarData = reader.ReadPacketSerialized<SerializableList<SyncVarData>>();
            Data = syncVarData.ContainedList;
            return reader;
        }
    }

    public struct SyncVarData : IPacketSerializable
    {
        public int NetworkIDTarget;

        public string TargetVar;

        public SerializedData Data;

        public OwnershipMode Mode;

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            NetworkIDTarget = reader.ReadInt();
            TargetVar = reader.ReadString();
            Data = reader.ReadPacketSerialized<SerializedData>();
            Mode = (OwnershipMode)reader.ReadByte();
            return reader.ReadBytes;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(NetworkIDTarget);
            writer.WriteString(TargetVar);
            writer.WritePacketSerialized<SerializedData>(Data);
            writer.WriteByte((byte)Mode);
            return writer.Data;
        }
    }
}
