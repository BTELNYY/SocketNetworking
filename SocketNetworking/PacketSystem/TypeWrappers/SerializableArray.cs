using Microsoft.Win32.SafeHandles;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.TypeWrappers
{
    public class SerializableArray<T> : IPacketSerializable
    {
        private T[] _array;

        private Type TType;

        public SerializableArray(T[] values)
        {
            if (!Packet.SupportedTypes.Contains(typeof(T)))
            {
                throw new ArgumentException("Array type is not supported, use one of the supported types instead.", "values");
            }
            _array = values;
            TType = typeof(T);
        }

        public Array ContainedArray => _array;

        public int Deserialize(byte[] data)
        {
            if(_array != null || _array.Length > 0)
            {
                throw new InvalidOperationException("The array must be empty in order to deserialize.");
            }
            int usedBytes = 0;
            int length = BitConverter.ToInt32(data, 0);
            byte[] arrayData = data.Take(length).ToArray();
            ByteReader reader = new ByteReader(arrayData);
            int lengthConfirmed = reader.ReadInt();
            usedBytes += 4;
            while (reader.DataLength > 0)
            {
                int currentChunkLength = reader.ReadInt();
                usedBytes += 4;
                byte[] readBytes = reader.Read(currentChunkLength);
                DeserializeAndAdd(readBytes);
                reader.Remove(currentChunkLength);
                usedBytes += currentChunkLength;
            }
            return usedBytes;
        }

        private void DeserializeAndAdd(byte[] data)
        {
            if (_array == null)
            {
                _array = new T[] { };
            }
            if (TType == typeof(IPacketSerializable))
            {
                T obj = (T)Activator.CreateInstance(TType);
                IPacketSerializable serializable = (IPacketSerializable)obj;
                serializable.Deserialize(data);
                _array.Append((T)serializable);
            }
            if(TType == typeof(string))
            {
                ByteReader reader = new ByteReader(data);
                string str = reader.ReadString();
                _array.Append((T)Convert.ChangeType(str, TType));
            }
            if (TType == typeof(bool))
            {
                ByteReader reader = new ByteReader(data);
                bool value = reader.ReadBool();
                _array.Append((T)Convert.ChangeType(value, TType));
            }
            if (TType == typeof(short))
            {
                ByteReader reader = new ByteReader(data);
                short value = reader.ReadShort();
                _array.Append((T)Convert.ChangeType(value, TType));
            }
            if (TType == typeof(int))
            {
                ByteReader reader = new ByteReader(data);
                int value = reader.ReadInt();
                _array.Append((T)Convert.ChangeType(value, TType));
            }
            if (TType == typeof(long))
            {
                ByteReader reader = new ByteReader(data);
                long value = reader.ReadLong();
                _array.Append((T)Convert.ChangeType(value, TType));
            }
            if (TType == typeof(ushort))
            {
                ByteReader reader = new ByteReader(data);
                ushort value = reader.ReadUShort();
                _array.Append((T)Convert.ChangeType(value, TType));
            }
            if (TType == typeof(uint))
            {
                ByteReader reader = new ByteReader(data);
                uint value = reader.ReadUInt();
                _array.Append((T)Convert.ChangeType(value, TType));
            }
            if(TType == typeof(ulong))
            {
                ByteReader reader = new ByteReader(data);
                ulong value = reader.ReadULong();
                _array.Append((T)Convert.ChangeType(value, TType));
            }
            if(TType == typeof(float))
            {
                ByteReader reader = new ByteReader(data);
                float value = reader.ReadFloat();
                _array.Append((T)Convert.ChangeType(value, TType));
            }
            if(TType == typeof(double))
            {
                ByteReader reader = new ByteReader(data);
                double value = reader.ReadDouble();
                _array.Append((T)Convert.ChangeType(value, TType));
            }
        }

        public int GetLength()
        {
            //We have this becuase our "counter" and "size" flags must exist. So the structure goes:
            //Length, Elements
            //Each element gets a length value attached to it.
            int size = sizeof(int);
            if (_array.Length == 0)
            {
                return size;
            }
            foreach(T element in _array)
            {
                size += sizeof(int);
                if(element.GetType().GetInterfaces().Contains(typeof(IPacketSerializable))) 
                {
                    IPacketSerializable serializable = (IPacketSerializable)element;
                    size += serializable.GetLength();
                }
                else
                {
                    size += element.GetType().SizeOf();
                }
            }
            return size;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(GetLength());
            foreach(T element in _array)
            {
                //Dont need to worry about type safety as we check in constructor
                byte[] finalBytes = Packet.SerializeSupportedType(element);
                int dataLength = finalBytes.Length;
                writer.WriteInt(dataLength);
                writer.Write(finalBytes);
            }
            return writer.Data;
        }
    }
}
