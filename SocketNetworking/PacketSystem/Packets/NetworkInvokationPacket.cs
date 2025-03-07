using System.Collections.Generic;
using SocketNetworking.PacketSystem.TypeWrappers;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class NetworkInvokationPacket : TargetedPacket
    {
        public override PacketType Type => PacketType.NetworkInvocation;

        public string TargetTypeAssmebly { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public string MethodName { get; set; } = string.Empty;

        public int CallbackID { get; set; } = 0;

        public bool IgnoreResult { get; set; } = false;

        public List<SerializedData> Arguments { get; set; } = new List<SerializedData>();

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteString(TargetTypeAssmebly);
            writer.WriteString(TargetType);
            writer.WriteString(MethodName);
            writer.WriteInt(CallbackID);
            writer.WriteBool(IgnoreResult);
            SerializableList<SerializedData> list = new SerializableList<SerializedData>();
            list.OverwriteContained(Arguments);
            writer.WritePacketSerialized<SerializableList<SerializedData>>(list);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            TargetTypeAssmebly = reader.ReadString();
            TargetType = reader.ReadString();
            MethodName = reader.ReadString();
            CallbackID = reader.ReadInt();
            IgnoreResult = reader.ReadBool();
            Arguments = reader.ReadPacketSerialized<SerializableList<SerializedData>>().ContainedList;
            return reader;
        }
    }
}
