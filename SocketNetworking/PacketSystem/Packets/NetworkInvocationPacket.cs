﻿using SocketNetworking.PacketSystem.TypeWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class NetworkInvocationPacket : Packet
    {
        public override PacketType Type => PacketType.NetworkInvocation;

        public override PacketFlags Flags
        {
            get
            {
                return PacketFlags.None;
            }
        }

        public string TargetTypeAssmebly { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public string MethodName { get; set; } = string.Empty;

        public int NetworkObjectTarget { get; set; } = 0;

        public int CallbackID { get; set; } = 0;

        public bool IgnoreResult { get; set; } = false;

        public List<SerializedData> Arguments { get; set; } = new List<SerializedData>();

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteString(TargetTypeAssmebly);
            writer.WriteString(TargetType);
            writer.WriteString(MethodName);
            writer.WriteInt(NetworkObjectTarget);
            writer.WriteInt(CallbackID);
            writer.WriteBool(IgnoreResult);
            SerializableList<SerializedData> list = new SerializableList<SerializedData>();
            list.OverwriteContained(Arguments);
            writer.Write<SerializableList<SerializedData>>(list);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            TargetTypeAssmebly = reader.ReadString();
            TargetType = reader.ReadString();
            MethodName = reader.ReadString();
            NetworkObjectTarget = reader.ReadInt();
            CallbackID = reader.ReadInt();
            IgnoreResult = reader.ReadBool();
            Arguments = reader.Read<SerializableList<SerializedData>>().ContainedList;
            return reader;
        }
    }
}
