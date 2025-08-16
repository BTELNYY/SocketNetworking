using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Packets.NetworkAnimator
{
    [PacketDefinition]
    public class NetworkAnimatorTriggerPacket : CustomPacket
    {
        public NetworkAnimatorTriggerPacket() : base() { }

        public int Hash { get; set; } = 0;

        public void WriteName(string name)
        {
            Hash = Animator.StringToHash(name);
        }

        public bool SetTrigger { get; set; } = false;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(Hash);
            writer.WriteBool(SetTrigger);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Hash = reader.ReadInt();
            SetTrigger = reader.ReadBool();
            return reader;
        }
    }
}
