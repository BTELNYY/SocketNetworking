using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using SocketNetworking.Shared.Exceptions;
using SocketNetworking.Shared.PacketSystem;

namespace SocketNetworking.Shared.Serialization
{
    /// <summary>
    /// Provides Packet reading capablities by reading elements of the byte array.
    /// </summary>
    public class ByteReader
    {
        object _lock = new object();

        /// <summary>
        /// Represents the original untouched data fed into the class. This data is not changed by any method.
        /// Meaning, This is the original data you gave in the constructor.
        /// </summary>
        public byte[] RawData { get; private set; }

        /// <summary>
        /// Amount of bytes that are unread
        /// </summary>
        public int DataLength
        {
            get
            {
                lock (_lock)
                {
                    return _workingSetData.Length;
                }
            }
        }

        /// <summary>
        /// Is the buffer empty?
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _workingSetData.Length == 0;
                }
            }
        }

        private byte[] _workingSetData = new byte[] { };

        /// <summary>
        /// Represents how many bytes have been removed from the working set compared to the original data, or how many bytes you have read from the array.
        /// </summary>
        public int ReadBytes
        {
            get
            {
                lock (_lock)
                {
                    return RawData.Length - _workingSetData.Length;
                }
            }
        }

        public ByteReader(byte[] data)
        {
            byte[] newBuff = new byte[data.Length];
            Array.Copy(data, newBuff, data.Length);
            RawData = newBuff;
            _workingSetData = RawData;
        }

        public ByteReader(byte[] data, bool showDebug) : this(data)
        {
            if (showDebug)
            {
                string result = Encoding.UTF8.GetString(data, 0, data.Length);
                Log.GlobalDebug(result);
            }
        }

        ~ByteReader()
        {
            _workingSetData = null;
            RawData = null;
        }

        public void Remove(int length)
        {
            lock (_lock)
            {
                int oldLength = _workingSetData.Length;
                _workingSetData = _workingSetData.RemoveFromStart(length);
                if(oldLength - length != _workingSetData.Length)
                {
                    throw new NetworkConversionException($"Remove From Start failed. Expected: {oldLength - length}, Got: {_workingSetData.Length}.");
                }
            }
        }

        public T ReadObject<T>()
        {
            lock (_lock)
            {
                try
                {
                    SerializedData data = ReadPacketSerialized<SerializedData>();
                    object obj = ByteConvert.Deserialize(data, out int read);
                    return (T)obj;
                }
                catch
                {
                    return default;
                }
            }
        }

        public byte[] Read(int length)
        {
            lock (_lock)
            {
                byte[] data = _workingSetData.Take(length).ToArray();
                Remove(length);
                return data;
            }
        }

        public byte[] ReadNoRemove(int length)
        {
            lock (_lock)
            {
                byte[] data = _workingSetData.Take(length).ToArray();
                return data;
            }
        }

        public byte[] ReadByteArray()
        {
            lock (_lock)
            {
                int length = ReadInt();
                if (length == 0)
                {
                    return new byte[0];
                }
                if (length > _workingSetData.Length)
                {
                    Log.GlobalWarning($"Read a byte array with a broken size. Read: {length}, Actual: {_workingSetData.Length}");
                    length = Math.Min(length, _workingSetData.Length);
                }
                return Read(length);
            }
        }

        public T ReadPacketSerialized<T>() where T : IPacketSerializable
        {
            lock (_lock)
            {
                IPacketSerializable serializable = (IPacketSerializable)Activator.CreateInstance(typeof(T));
                int bytesUsed = serializable.Deserialize(_workingSetData).ReadBytes;
                Remove(bytesUsed);
                return (T)serializable;
            }
        }

        public K ReadWrapper<T, K>() where T : TypeWrapper<K>
        {
            lock (_lock)
            {
                byte[] bytes = ReadByteArray();
                TypeWrapper<K> wrapper = (TypeWrapper<K>)Activator.CreateInstance(typeof(T));
                ValueTuple<K, int> result = wrapper.Deserialize(bytes);
                //Remove(result.Item2);
                return result.Item1;
            }
        }

        public T ReadWrapper<T>()
        {
            lock (_lock)
            {
                Type type = typeof(T);
                if (!NetworkManager.TypeToTypeWrapper.ContainsKey(type))
                {
                    throw new InvalidOperationException("No type wrapper for type: " + type.FullName);
                }
                byte[] bytes = ReadByteArray();
                object typeWrapper = Activator.CreateInstance(NetworkManager.TypeToTypeWrapper[type]);
                MethodInfo deserializer = typeWrapper.GetType().GetMethod("Deserialize");
                ValueTuple<T, int> result = (ValueTuple<T, int>)deserializer.Invoke(typeWrapper, new object[] { bytes });
                //Remove(result.Item2);
                return result.Item1;
            }
        }


        public byte ReadByte()
        {
            lock (_lock)
            {
                byte result = _workingSetData[0];
                Remove(1);
                return result;
            }
        }

        public sbyte ReadSByte()
        {
            lock (_lock)
            {
                sbyte result = Convert.ToSByte(ReadByte());
                return result;
            }
        }

        public ulong ReadULong()
        {
            lock (_lock)
            {
                int sizeToRemove = sizeof(ulong);
                ulong result = BitConverter.ToUInt64(_workingSetData, 0);
                Remove(sizeToRemove);
                return (ulong)IPAddress.NetworkToHostOrder((long)result);
            }
        }

        public uint ReadUInt()
        {
            lock (_lock)
            {
                int sizeToRemove = sizeof(uint);
                uint result = BitConverter.ToUInt32(_workingSetData, 0);
                Remove(sizeToRemove);
                return (uint)IPAddress.NetworkToHostOrder((int)result);
            }
        }

        public ushort ReadUShort()
        {
            lock (_lock)
            {
                int sizeToRemove = sizeof(ushort);
                ushort result = BitConverter.ToUInt16(_workingSetData, 0);
                Remove(sizeToRemove);
                return (ushort)IPAddress.NetworkToHostOrder((short)result);
            }
        }

        public long ReadLong()
        {
            lock (_lock)
            {
                int sizeToRemove = sizeof(long);
                long result = BitConverter.ToInt64(_workingSetData, 0);
                Remove(sizeToRemove);
                return IPAddress.NetworkToHostOrder(result);
            }
        }

        public int ReadInt()
        {
            lock (_lock)
            {
                int sizeToRemove = sizeof(int);
                int result = BitConverter.ToInt32(Read(sizeToRemove), 0);
                int networkResult = IPAddress.NetworkToHostOrder(result);
                return networkResult;
            }
        }

        public short ReadShort()
        {
            lock (_lock)
            {
                int sizeToRemove = sizeof(short);
                short result = BitConverter.ToInt16(_workingSetData, 0);
                Remove(sizeToRemove);
                return IPAddress.NetworkToHostOrder(result);
            }
        }

        public float ReadFloat()
        {
            lock (_lock)
            {
                int sizeToRemove = sizeof(float);
                float result = BitConverter.ToSingle(_workingSetData, 0);
                Remove(sizeToRemove);
                return result;
            }
        }

        public double ReadDouble()
        {
            lock (_lock)
            {
                int sizeToRemove = sizeof(double);
                double result = BitConverter.ToDouble(_workingSetData, 0);
                Remove(sizeToRemove);
                return result;
            }
        }

        public string ReadString()
        {
            lock (_lock)
            {
                byte[] stringBuff = ReadByteArray();
                if (stringBuff.Length == 0)
                {
                    return "";
                }
                List<char> cChars = Encoding.UTF32.GetChars(stringBuff).ToList();
                string result = new string(cChars.ToArray());
                Log.GlobalDebug(result);
                return result;
            }
        }

        public bool ReadBool()
        {
            lock (_lock)
            {
                int sizeToRemove = sizeof(bool);
                bool result = BitConverter.ToBoolean(_workingSetData, 0);
                Remove(sizeToRemove);
                return result;
            }
        }
    }
}
