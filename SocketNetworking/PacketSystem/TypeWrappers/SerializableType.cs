using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;

namespace SocketNetworking.PacketSystem.TypeWrappers
{
    [TypeWrapper(typeof(Type))]
    public class SerializableType : TypeWrapper<Type>
    {
        public override (Type, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            string sType = reader.ReadString();
            string sAssmebly = reader.ReadString();
            Type result = Assembly.Load(sAssmebly).GetType(sType);
            Value = result;
            return (result, reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(Value.FullName);
            writer.WriteString(Value.Assembly.FullName);
            return writer.Data;
        }
    }
}
