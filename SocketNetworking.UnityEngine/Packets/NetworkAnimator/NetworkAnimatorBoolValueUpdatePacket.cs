using System;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Packets.NetworkAnimator
{
    [PacketDefinition]
    public class NetworkAnimatorBoolValueUpdatePacket : CustomPacket
    {
        public NetworkAnimatorBoolValueUpdatePacket() { }

        public NetworkAnimatorBoolValueUpdatePacket(int id, bool value)
        {
            ValueHash = id;
            Value = value;
        }

        public NetworkAnimatorBoolValueUpdatePacket(string name, bool value) 
        {
            ValueHash = Animator.StringToHash(name);
            Value = value;
        }

        public int ValueHash { get; set; } = 0;

        public bool Value { get; set; } = false;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(ValueHash);
            writer.WriteInt(Convert.ToInt32(Value));
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            ValueHash = reader.ReadInt();
            Value = Convert.ToBoolean(reader.ReadInt());
            return reader;
        }
    }
}
