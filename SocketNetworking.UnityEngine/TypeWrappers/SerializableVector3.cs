using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    public class SerializableVector3 : IByteSerializable
    {
        public Vector3 Vector;

        public SerializableVector3()
        {
            Vector = new Vector3();
        }

        public SerializableVector3(Vector3 vector)
        {
            Vector = vector;
        }

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Vector = reader.ReadVector3();
            return reader;
        }

        public int GetLength()
        {
            return sizeof(float) * 3;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteVector3(Vector);
            return writer;
        }
    }
}
