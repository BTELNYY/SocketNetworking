using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.UnityEngine.Packets.NetworkAnimator
{
    [PacketDefinition]
    public class NetworkAnimatorPlayAnimPacket : CustomPacket
    {
        public bool IsStateHash { get; set; } = false;

        public bool DoNotPlayAnything { get; set; } = false;

        public string StateName { get; set; } = string.Empty;

        public int Layer { get; set; } = -1;

        public float NormalizedTime { get; set; } = float.NegativeInfinity;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteBool(IsStateHash);
            writer.WriteBool(DoNotPlayAnything);
            writer.WriteString(StateName);
            writer.WriteInt(Layer);
            writer.WriteFloat(NormalizedTime);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            IsStateHash = reader.ReadBool();
            DoNotPlayAnything = reader.ReadBool();
            StateName = reader.ReadString();
            Layer = reader.ReadInt();
            NormalizedTime = reader.ReadFloat();
            return reader;
        }
    }
}
