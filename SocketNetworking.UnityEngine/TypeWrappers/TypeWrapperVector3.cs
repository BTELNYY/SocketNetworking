using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    [TypeWrapper(typeof(Vector3))]
    public class TypeWrapperVector3 : TypeWrapper<Vector3>
    {
        public override (Vector3, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Value = reader.ReadVector3();
            return (Value, reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteVector3(Value);
            return writer.Data;
        }
    }
}
