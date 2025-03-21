using System;
using System.Collections.Generic;
using SocketNetworking.Shared.PacketSystem.TypeWrappers;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class NetworkInvocationPacket : TargetedPacket
    {
        public override PacketType Type => PacketType.NetworkInvocation;

        public Type TargetType { get; set; }

        public string MethodName { get; set; } = string.Empty;

        public int CallbackID { get; set; } = 0;

        public bool IgnoreResult { get; set; } = false;

        public List<SerializedData> Arguments { get; set; } = new List<SerializedData>();

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteWrapper<SerializableType, Type>(new SerializableType(TargetType));
            writer.WriteString(MethodName);
            writer.WriteInt(CallbackID);
            writer.WriteBool(IgnoreResult);
            SerializableList<SerializedData> list = new SerializableList<SerializedData>();
            list.OverwriteContained(Arguments);
            writer.WritePacketSerialized<SerializableList<SerializedData>>(list);
            //Log.GlobalDebug(ToString());
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            TargetType = reader.ReadWrapper<SerializableType, Type>();
            MethodName = reader.ReadString();
            CallbackID = reader.ReadInt();
            IgnoreResult = reader.ReadBool();
            Arguments = reader.ReadPacketSerialized<SerializableList<SerializedData>>().ContainedList;
            return reader;
        }

        public override string ToString()
        {
            return base.ToString() + $"TargetType: {TargetType}, MethodName: {MethodName}, CallbackID: {CallbackID}, IgnoreResult: {IgnoreResult}, Arguments: ({string.Join(" / ", Arguments)}";
        }
    }
}
