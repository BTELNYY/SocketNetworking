using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Packets.NetworkTransform
{
    [PacketDefinition]
    public class NetworkTransformRotatePacket : NetworkTransformBasePacket
    {
        public NetworkTransformRotatePacket() : base() { }

        public Vector3 Rotation { get; set; }

        public Space Space { get; set; }

        public float Angle { get; set; } = float.NaN;

        public NetworkTransformRotatePacket(Vector3 rotation, Space space)
        {
            Rotation = rotation;
            Space = space;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteVector3(Rotation);
            writer.WriteByte((byte)Space);
            writer.WriteFloat(Angle);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Rotation = reader.ReadVector3();
            Space = (Space)reader.ReadByte();
            Angle = reader.ReadFloat();
            return reader;
        }
    }
}
