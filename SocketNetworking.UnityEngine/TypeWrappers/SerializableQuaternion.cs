using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    [TypeWrapper(typeof(Quaternion))]
    public class SerializableQuaternion : TypeWrapper<Quaternion>
    {
        public override (Quaternion, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            return (reader.ReadQuaternion(), reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteQuaternion(Value);
            return writer.Data;
        }
    }
}
