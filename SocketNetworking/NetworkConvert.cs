using SocketNetworking.PacketSystem;
using SocketNetworking.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem.TypeWrappers;
using System.Collections;

namespace SocketNetworking
{
    public class NetworkConvert
    {
        public static SerializedData Serialize<T>(T data)
        {
            ByteWriter writer = new ByteWriter();
            Type dataType = typeof(T);
            SerializedData sData = new SerializedData();
            sData.Type = dataType;

            if(data is IPacketSerializable serializable)
            {
                writer.Write<T>(serializable);
                return sData;
            }
            if(dataType.IsAssignableFrom(typeof(IEnumerable)))
            {
                IEnumerable<object> values = (IEnumerable<object>) data;
                SerializableList<object> list= new SerializableList<object>(values);
                writer.Write<SerializableList<object>>(list);
                return sData;
            }

            if(dataType == typeof(string))
            {
                string value = (string)Convert.ChangeType(data, typeof(string));
                writer.WriteString(value);
                return sData;
            }
            if(dataType == typeof(bool))
            {
                bool value = (bool)Convert.ChangeType(data, typeof(bool));
                writer.WriteBool(value);
                return sData;
            }

            if(dataType == typeof(byte))
            {
                byte value = (byte)Convert.ChangeType(data, typeof(byte));
                writer.WriteByte(value);
                return sData;
            }
            if(dataType == typeof(sbyte))
            {
                sbyte value = (sbyte)Convert.ChangeType(data, typeof(sbyte));
                writer.WriteSByte(value);
                return sData;
            }

            if(dataType == typeof(int))
            {
                int value = (int)Convert.ChangeType(data, typeof(int));
                writer.WriteInt(value);
                return sData;
            }
            if (dataType == typeof(uint))
            {
                uint value = (uint)Convert.ChangeType(data, typeof(uint));
                writer.WriteUInt(value);
                return sData;
            }

            if(dataType == typeof(long))
            {
                long value = (long)Convert.ChangeType(data, typeof(long));
                writer.WriteLong(value);
                return sData;
            }
            if (dataType == typeof(ulong))
            {
                ulong value = (ulong)Convert.ChangeType(data, typeof(ulong));
                writer.WriteULong(value);
                return sData;
            }

            if(dataType == typeof(float))
            {
                float value = (float)Convert.ChangeType(data, typeof(float));
                writer.WriteFloat(value);
                return sData;
            }
            if(dataType == typeof(double))
            {
                double value = (float)Convert.ChangeType(data, typeof(double));
                writer.WriteDouble(value);
                return sData;
            }

            return sData;
        }
        
        public static T DeserializeRaw<T>(byte[] data)
        {
            SerializedData sData = new SerializedData()
            {
                Data = data,
                Type = typeof(T),
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
