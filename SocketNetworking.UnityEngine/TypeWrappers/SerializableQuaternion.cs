using SocketNetworking.PacketSystem;
using SocketNetworking.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Quaternion = reader.ReadQuaternion();
            return reader.ReadBytes;
        }

        public int GetLength()
        {
            return sizeof(float) * 4;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteQuaternion(Quaternion);
            return writer.Data;
        }
    }
}
