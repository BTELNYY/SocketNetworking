using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    [TypeWrapper(typeof(Quaternion))]
    public class TypeWrapperQuaternion : TypeWrapper<Quaternion>
    {
        public override (Quaternion, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Value = reader.ReadQuaternion();
            return (Value, reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteQuaternion(Value);
            return writer.Data;
        }
    }
}
