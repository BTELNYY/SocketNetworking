using SocketNetworking.PacketSystem;
using SocketNetworking.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem.TypeWrappers;

namespace SocketNetworking
{
    public class NetworkConvert
    {
        public static ByteWriter Serialize<T>(T data)
        {
            ByteWriter writer = new ByteWriter();
            Type dataType = typeof(T);
            SerializedData sData = new SerializedData();
            sData.Type = dataType;
            return null;
        }
        
        public static T Deserialize<T>(Type t, byte[] data)
        {
            SerializedData sData = new SerializedData()
            {
                Data = data,
                Type = t,
            };
            return Deserialize<T>(sData, out int read);
        }

        public static T Deserialize<T>(SerializedData data, out int read)
        {
            if(data.Type != typeof(T))
            {
                throw new NetworkDeserializationException("Types provided do not match.");
            }
            ByteReader reader = new ByteReader(data.Data);
            if (data.Type.IsAssignableFrom(typeof(IPacketSerializable)))
            {
                T obj = (T)Activator.CreateInstance(data.Type);
                IPacketSerializable serializable = (IPacketSerializable)obj;
                read = serializable.Deserialize(data.Data);
                return (T)serializable;
            }

            if (data.Type == typeof(string))
            {
                string str = reader.ReadString();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(str, typeof(T));
            }
            if (data.Type == typeof(bool))
            {
                bool value = reader.ReadBool();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }

            if (data.Type == typeof(byte))
            {
                byte value = reader.ReadByte();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            if (data.Type == typeof(sbyte))
            {
                sbyte value = reader.ReadSByte();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }

            if (data.Type == typeof(short))
            {
                short value = reader.ReadShort();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            if (data.Type == typeof(ushort))
            {
                ushort value = reader.ReadUShort();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }

            if (data.Type == typeof(int))
            {
                int value = reader.ReadInt();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            if (data.Type == typeof(uint))
            {
                uint value = reader.ReadUInt();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }

            if (data.Type == typeof(long))
            {
                long value = reader.ReadLong();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            if (data.Type == typeof(ulong))
            {
                ulong value = reader.ReadULong();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }

            if (data.Type == typeof(float))
            {
                float value = reader.ReadFloat();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            if (data.Type == typeof(double))
            {
                double value = reader.ReadDouble();
                read = reader.ReadBytes;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            read = 0;
            return default;
        }

        public static T Deserialize<T>(byte[] data)
        {
            ByteReader br = new ByteReader(data);
            SerializedData sData = br.Read<SerializedData>();
            if(!br.IsEmpty)
            {
                Log.GlobalWarning("Provided Data Array was not emptied by the deseiralizer, probably extra bytes?");
            }
            Type givenType = sData.Type;
            if(givenType == null)
            {
                throw new NetworkDeserializationException($"Type {sData.TypeFullName} cannnot be found.");
            }
            if (!typeof(T).IsAssignableFrom(givenType))
            {
                throw new NetworkDeserializationException("Given Type is not Assignable from the deserialized type.");
            }
            return Deserialize<T>(sData, out int read);
        }
    }

    public struct SerializedData : IPacketSerializable
    {
        public string TypeFullName;

        public Type Type
        {
            get
            {
                return System.Type.GetType(TypeFullName);
            }
            set
            {
                TypeFullName = value.FullName;
            }
        }

        public byte[] Data;

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            TypeFullName = reader.ReadString();
            Data = reader.ReadByteArray();
            return reader.ReadBytes;
        }

        public int GetLength()
        {
            return TypeFullName.SerializedSize() + Data.Length;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(TypeFullName);
            writer.WriteByteArray(Data);
            return writer.Data;
        }
    }
}
