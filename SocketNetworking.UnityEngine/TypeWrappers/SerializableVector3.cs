using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    public class SerializableVector3 : IPacketSerializable
    {
        public Vector3 Vector;

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Vector = reader.ReadVector3();
            return reader.ReadBytes;
        }

        public int GetLength()
        {
            return sizeof(float) * 3;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteVector3(Vector);
            return writer.Data;
        }
    }
}
