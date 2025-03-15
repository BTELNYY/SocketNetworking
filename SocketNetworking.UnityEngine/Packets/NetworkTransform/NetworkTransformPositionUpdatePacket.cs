using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Packets.NetworkTransform
{
    [PacketDefinition]
    public class NetworkTransformPositionUpdatePacket : NetworkTransformBasePacket
    {
        public Vector3 Position { get; set; } = Vector3.zero;

        public Quaternion Rotation { get; set; } = new Quaternion(0, 0, 0, 0);

        public bool Local { get; set; } = false;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteBool(Local);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            Local = reader.ReadBool();
            return reader;
        }
    }
}
