using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    [TypeWrapper(typeof(Vector3Int))]
    public class SerializableVector3Int : TypeWrapper<Vector3Int>
    {
        public override (Vector3Int, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            return (new Vector3Int(reader.ReadInt(), reader.ReadInt(), reader.ReadInt()), reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(Value.x);
            writer.WriteInt(Value.y);
            writer.WriteInt(Value.z);
            return writer.Data;
        }
    }
}
