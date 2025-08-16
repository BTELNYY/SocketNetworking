using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Packets.NetworkTransform
{
    [PacketDefinition]
    public class NetworkTransformRotateAroundPacket : NetworkTransformBasePacket
    {
        public NetworkTransformRotateAroundPacket() : base() { }

        public Vector3 Point { get; set; }

        public Vector3 Axis { get; set; }

        public float Angle { get; set; }

        public byte AroundType { get; set; }

        public NetworkTransformRotateAroundPacket(Vector3 point, Vector3 axis, float angle)
        {
            Point = point;
            Axis = axis;
            Angle = angle;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteVector3(Point);
            writer.WriteVector3(Axis);
            writer.WriteFloat(Angle);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Point = reader.ReadVector3();
            Axis = reader.ReadVector3();
            Angle = reader.ReadFloat();
            return reader;
        }
    }
}
