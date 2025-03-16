using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    [TypeWrapper(typeof(Vector2Int))]
    public class SerializableVector2Int : TypeWrapper<Vector2Int>
    {
        public override (Vector2Int, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            return (reader.ReadVector2Int(), reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteVector2Int(Value);
            return writer.Data;
        }
    }
}
