using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    [TypeWrapper(typeof(Vector2))]
    public class TypeWrapperVector2 : TypeWrapper<Vector2>
    {
        public override (Vector2, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Value = reader.ReadVector2();
            return (Value, reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteVector2(Value);
            return writer.Data;
        }
    }
}
