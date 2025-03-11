using SocketNetworking.Attributes;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.UnityEngine.Packets.NetworkAnimator
{
    [PacketDefinition]
    public class NetworkAnimatorSpeedUpdatePacket : CustomPacket
    {
        public float AnimatorSpeed { get; set; } = 0f;

        public NetworkAnimatorSpeedUpdatePacket()
        {

        }

        public NetworkAnimatorSpeedUpdatePacket(float animatorSpeed)
        {
            AnimatorSpeed = animatorSpeed;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteFloat(AnimatorSpeed);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            AnimatorSpeed = reader.ReadFloat();
            return reader;
        }
    }
}
