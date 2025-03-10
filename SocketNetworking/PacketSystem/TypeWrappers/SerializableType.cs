using System;
using System.Reflection;
using SocketNetworking.Attributes;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.PacketSystem.TypeWrappers
{
    [TypeWrapper(typeof(Type))]
    public class SerializableType : TypeWrapper<Type>
    {
        public SerializableType() { }

        public SerializableType(Type type)
        {
            if(type == null)
            {
                type = typeof(void);
            }
            Value = type;
        }

        public override (Type, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            string sType = reader.ReadString();
            bool isHash = reader.ReadBool();
            Type result;
            if(isHash)
            {
                ulong hash = reader.ReadULong();
                Assembly assembly = NetworkManager.GetAssemblyFromHash(hash);
                if(assembly != null)
                {
                    result = assembly.GetType(sType);
                }
                else
                {
                    throw new InvalidOperationException($"Unable to find an assembly with hash {hash}");
                }
            }
            else
            {
                string sAssmebly = reader.ReadString();
                result = Assembly.Load(sAssmebly).GetType(sType);
            }
            Value = result;
            return (result, reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(Value.FullName);
            if(NetworkManager.HasAssemblyHash(Value.Assembly))
            {
                writer.WriteBool(true);
                writer.WriteULong(NetworkManager.GetHashFromAssembly(Value.Assembly));
            }
            else
            {
                writer.WriteBool(false);
                writer.WriteString(Value.Assembly.FullName);
            }
            return writer.Data;
        }
    }
}
