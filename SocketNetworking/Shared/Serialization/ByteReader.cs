using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using SocketNetworking.Shared.Exceptions;

namespace SocketNetworking.Shared.Serialization
{
    /// <summary>
    /// Provides Packet reading capabilities by reading values from the contained byte array.
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

        /// <summary>
        /// Removes a specific amount of <see langword="byte"/>s from the buffer.
        /// </summary>
        /// <param name="length"></param>
        /// <exception cref="NetworkConversionException"></exception>
        public void Remove(int length)
        {
            lock (_lock)
            {
                int oldLength = _workingSetData.Length;
                _workingSetData = _workingSetData.RemoveFromStart(length);
                if (oldLength - length != _workingSetData.Length)
                {
                    throw new NetworkConversionException($"Remove From Start failed. Expected: {oldLength - length}, Got: {_workingSetData.Length}.");
                }
            }
        }

        /// <summary>
        /// Tries to read a <see cref="SerializedData"/> object and then casts its contained value to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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

        /// <summary>
        /// Reads <paramref name="length"/> <see cref="byte"/>s from the buffer, then calls the <see cref="Remove(int)"/> method to remove <paramref name="length"/> <see cref="byte"/>s.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] Read(int length)
        {
            lock (_lock)
            {
                byte[] data = _workingSetData.Take(length).ToArray();
                Remove(length);
                return data;
            }
        }

        /// <summary>
        /// Does the same as <see cref="Read(int)"/>, but does not call <see cref="Remove(int)"/>.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] ReadNoRemove(int length)
        {
            lock (_lock)
            {
                byte[] data = _workingSetData.Take(length).ToArray();
                return data;
            }
        }

        /// <summary>
        /// Reads a <see langword="byte"/> array and removes it from the buffer. Byte arrays are stored as an <see cref="int"/> for the length followed by the bytes making up the byte array.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="IByteSerializable"/> from the buffer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ReadPacketSerialized<T>() where T : IByteSerializable
        {
            lock (_lock)
            {
                IByteSerializable serializable = (IByteSerializable)Activator.CreateInstance(typeof(T));
                int bytesUsed = serializable.Deserialize(_workingSetData).ReadBytes;
                Remove(bytesUsed);
                return (T)serializable;
            }
        }

        /// <summary>
        /// Reads a <see cref="TypeWrapper{T}"/> from the buffer. <typeparamref name="K"/> is the final type that this method returns, and <typeparamref name="T"/> is the <see cref="TypeWrapper{T}"/> which has a generic argument of <typeparamref name="K"/>. The <see cref="TypeWrapper{T}"/> does not need to be registered in <see cref="NetworkManager.TypeToTypeWrapper"/>. if it is, you can use <see cref="ReadWrapper{T}()"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="TypeWrapper{T}"/> that has been registered in <see cref="NetworkManager.TypeToTypeWrapper"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
                ITypeWrapper wrapper = (ITypeWrapper)typeWrapper;
                ValueTuple<T, int> result = ((T, int))wrapper.DeserializeRaw(bytes);
                //Remove(result.Item2);
                return result.Item1;
            }
        }

        /// <summary>
        /// Reads a <see cref="byte"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            lock (_lock)
            {
                byte result = _workingSetData[0];
                Remove(1);
                return result;
            }
        }

        /// <summary>
        /// Reads a <see cref="sbyte"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
        public sbyte ReadSByte()
        {
            lock (_lock)
            {
                sbyte result = Convert.ToSByte(ReadByte());
                return result;
            }
        }

        /// <summary>
        /// Reads a <see cref="ulong"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="uint"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="ushort"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="long"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="int"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="short"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="float"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="double"/> from the buffer and removes it.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a <see cref="string"/> from the buffer and removes it. This method uses <see cref="ReadByteArray"/> to retrieve the <see cref="Encoding.UTF32"/> encoded string.
        /// </summary>
        /// <returns></returns>
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
                //Log.GlobalDebug(result);
                return result;
            }
        }

        /// <summary>
        /// Reads a <see cref="bool"/> from the buffer and removes it. You should use the <see cref="FlagsAttribute"/> on an <see cref="Enum"/> if you are trying to serialize a large amount of <see cref="bool"/>s as a single <see cref="bool"/> is 1 <see cref="byte"/> large.
        /// </summary>
        /// <returns></returns>
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
