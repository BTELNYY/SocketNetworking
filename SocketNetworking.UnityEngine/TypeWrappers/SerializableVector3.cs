using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    [TypeWrapper(typeof(Vector3))]
    public class SerializableVector3 : TypeWrapper<Vector3>
    {
        public override (Vector3, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            return (reader.ReadVector3(), reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteVector3(Value);
            return writer.Data;
        }
    }
}
