using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    public class SerializableQuaternion : IPacketSerializable
    {
        public Quaternion Quaternion;

        public SerializableQuaternion()
        {
            Quaternion = new Quaternion();
        }

        public SerializableQuaternion(Quaternion quaternion)
        {
            Quaternion = quaternion;
        }

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Quaternion = reader.ReadQuaternion();
            return reader;
        }

        public int GetLength()
        {
            return sizeof(float) * 4;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteQuaternion(Quaternion);
            return writer;
        }
    }
}
