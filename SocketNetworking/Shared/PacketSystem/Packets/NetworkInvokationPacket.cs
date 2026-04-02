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

        [Obsolete]
        public string MethodName { get; set; } = string.Empty;

        public int MethodIndex { get; set; } = 0;

        public int CallbackID { get; set; } = 0;

        public bool IgnoreResult { get; set; } = false;

        public List<SerializedData> Arguments { get; set; } = new List<SerializedData>();

#pragma warning disable CS0612 // Type or member is obsolete
        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteWrapper<SerializableType, Type>(new SerializableType(TargetType));
            writer.WriteString(MethodName);
            writer.WriteInt(MethodIndex);
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
            MethodIndex = reader.ReadInt();
            CallbackID = reader.ReadInt();
            IgnoreResult = reader.ReadBool();
            Arguments = reader.ReadPacketSerialized<SerializableList<SerializedData>>().ContainedList;
            return reader;
        }

        public override string ToString()
        {
            return base.ToString() + $"TargetType: {TargetType}, MethodName: {MethodName}, CallbackID: {CallbackID}, IgnoreResult: {IgnoreResult}, Arguments: ({string.Join(" / ", Arguments)}";
        }
#pragma warning restore CS0612 // Type or member is obsolete
    }
}
