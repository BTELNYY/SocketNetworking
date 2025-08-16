using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Packets.NetworkAnimator
{
    [PacketDefinition]
    public class NetworkAnimatorIntValueUpdatePacket : CustomPacket
    {
        public NetworkAnimatorIntValueUpdatePacket() : base() { }

        public NetworkAnimatorIntValueUpdatePacket(int id, int value) : this()
        {
            ValueHash = id;
            Value = value;
        }

        public NetworkAnimatorIntValueUpdatePacket(string name, int value) : this()
        {
            ValueHash = Animator.StringToHash(name);
            Value = value;
        }

        public int ValueHash { get; set; } = 0;

        public int Value { get; set; } = 0;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(ValueHash);
            writer.WriteInt(Value);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            ValueHash = reader.ReadInt();
            Value = reader.ReadInt();
            return reader;
        }
    }
}
