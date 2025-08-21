using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using SocketNetworking.Shared.Exceptions;

namespace SocketNetworking.Shared.Serialization
{
    /// <summary>
    /// Provides buffer manipulation methods.
    /// </summary>
    public class ByteWriter
    {
        object _lock = new object();

        /// <summary>
        /// The current length of the buffer.
        /// </summary>
        public long Length
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
        /// The written data buffer.
        /// </summary>
        public byte[] Data
        {
            get
            {
                lock (_lock)
                {
                    return _workingSetData;
                }
            }
        }

        private byte[] _workingSetData = new byte[] { };

        public ByteWriter()
        {
            _workingSetData = new byte[0];
        }

        public ByteWriter(byte[] existingData)
        {
            _workingSetData = existingData;
        }


        private Stream _stream;

        public ByteWriter(Stream stream)
        {
            this._stream = stream;
        }

        private long written;

        ~ByteWriter()
        {
            _workingSetData = null;
        }

        /// <summary>
        /// Writes a <see cref="byte"/> array to the buffer. No prefix is added.
        /// </summary>
        /// <param name="data"></param>
        public void Write(byte[] data)
        {
            lock (_lock)
            {
                written += data.LongLength;
                if (_stream == null)
                {
                    _workingSetData = _workingSetData.FastConcat(data).ToArray();
                }
                else
                {
                    _stream.Write(data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Writes the XML version of <paramref name="value"/> as a <see langword="string"/>. If serialization fails, <see cref="string.Empty"/> will be written.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void WriteXML<T>(T value)
        {
            string xml = value.ToXML<T>();
            WriteString(xml);
        }

        /// <summary>
        /// Identical to <see cref="WriteXML{T}(T)"/> but does not use generics.
        /// </summary>
        /// <param name="value"></param>
        public void WriteXML(object value)
        {
            string xml = value.ToXML();
            WriteString(xml);
        }

        /// <summary>
        /// Writes the <paramref name="value"/> by wrapping it in a <see cref="SerializedData"/> struct.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void WriteObject<T>(T value)
        {
            lock (_lock)
            {
                SerializedData data = ByteConvert.Serialize(value);
                WritePacketSerialized<SerializedData>(data);
            }
        }

        /// <summary>
        /// Writes a <see cref="IByteSerializable"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializable"></param>
        public void WritePacketSerialized<T>(IByteSerializable serializable)
        {
            lock (_lock)
            {
                byte[] data = serializable.Serialize().Data;
                WriteInt(data.Length);
                Write(data);
            }
        }

        /// <summary>
        /// Writes <paramref name="any"/> to the buffer by trying to find a <see cref="TypeWrapper{T}"/> for it. If none is found, a <see cref="NetworkConversionException"/> is thrown.
        /// </summary>
        /// <param name="any"></param>
        /// <exception cref="NetworkConversionException"></exception>
        public void WriteWrapper(object any)
        {
            lock (_lock)
            {
                Type type = any.GetType();
                if (!NetworkManager.TypeToTypeWrapper.ContainsKey(type))
                {
                    throw new NetworkConversionException("No type wrapper for type: " + type.FullName);
                }
                object wrapper = Activator.CreateInstance(NetworkManager.TypeToTypeWrapper[type]);
                //Hacky fix.
                MethodInfo serializer = wrapper.GetType().GetMethod("Serialize");
                byte[] result = (byte[])serializer.Invoke(wrapper, new object[] { any });
                WriteByteArray(result);
            }
        }

        /// <summary>
        /// Writes <paramref name="value"/> using the provided <see cref="TypeWrapper{T}"/> as <typeparamref name="T"/>. <typeparamref name="K"/> is the type of <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="value"></param>
        public void WriteWrapper<T, K>(T value) where T : TypeWrapper<K>
        {
            lock (_lock)
            {
                byte[] data = value.Serialize();
                WriteByteArray(data);
            }
        }

        /// <summary>
        /// Writes a <see cref="byte"/> array to the buffer. Unlike <see cref="Write(byte[])"/>, this method will prepend an <see cref="int"/> to represent the length of the <paramref name="data"/>. Throws an <see cref="NetworkConversionException"/> if a failsafe condition is triggered where the length of the <paramref name="data"/> + 4 + the old buffer length does not equal <see cref="Length"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NetworkConversionException"></exception>
        public void WriteByteArray(byte[] data)
        {
            lock (_lock)
            {
                int oldLength = _workingSetData.Length;
                int expectedLength = 4 + data.Length + _workingSetData.Length;
                WriteInt(data.Length);
                if (data.Length == 0)
                {
                    return;
                }
                Write(data);
                if (_workingSetData.Length != expectedLength)
                {
                    throw new NetworkConversionException($"Wrote an invalid byte array! Expected: {expectedLength}, Actual: {_workingSetData.Length}");
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="byte"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteByte(byte data)
        {
            lock (_lock)
            {
                Write(new byte[] { data });
                //_workingSetData = _workingSetData.Append(data).ToArray();
            }
        }

        /// <summary>
        /// Writes a <see cref="sbyte"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteSByte(sbyte data)
        {
            lock (_lock)
            {
                byte written = Convert.ToByte(data);
                
                _workingSetData = _workingSetData.Concat(new byte[] { written }).ToArray();
            }
        }

        /// <summary>
        /// Writes a <see cref="long"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteLong(long data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data));
                Write(result);
            }
        }

        /// <summary>
        /// Writes a <see cref="int"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteInt(int data)
        {
            lock (_lock)
            {
                int network = IPAddress.HostToNetworkOrder(data);
                byte[] result = BitConverter.GetBytes(network);
                Write(result);
            }
        }

        /// <summary>
        /// Writes a <see cref="short"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteShort(short data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data));
                Write(result);
            }
        }

        /// <summary>
        /// Writes a <see cref="ulong"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteULong(ulong data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)data));
                Write(result);
            }
        }

        /// <summary>
        /// Writes a <see cref="uint"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteUInt(uint data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)data));
                Write(result);
            }
        }

        /// <summary>
        /// Writes a <see cref="ushort"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteUShort(ushort data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)data));
                Write(result);
            }
        }

        /// <summary>
        /// Writes a <see cref="float"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteFloat(float data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(data);
                Write(result);
            }
        }

        /// <summary>
        /// Writes a <see cref="double"/> to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void WriteDouble(double data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(data);
                Write(result);
            }
        }

        /// <summary>
        /// Writes a <see cref="string"/> to the buffer. This method uses <see cref="WriteByteArray(byte[])"/> to write the <see cref="Encoding.UTF32"/> formatted string. Throws an <see cref="NetworkConversionException"/> if a failsafe condition is triggered where the new buffer length is not equal to the old buffer length + <see cref="string"/> byte array length.
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NetworkConversionException"></exception>
        public void WriteString(string data)
        {
            lock (_lock)
            {
                byte[] bytes = Encoding.UTF32.GetBytes(data);
                int oldLength = _workingSetData.Length;
                int expectedLength = bytes.Length + 4;
                WriteByteArray(bytes);
                if (oldLength + expectedLength != _workingSetData.Length)
                {
                    throw new NetworkConversionException("StringWriter error.");
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="bool"/> to the buffer. If you are writing a large amount of <see cref="bool"/>s, you should check out the <see cref="FlagsAttribute"/> for the <see cref="Enum"/> type as 1 <see cref="bool"/> is 1 byte.
        /// </summary>
        /// <param name="data"></param>
        public void WriteBool(bool data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(data);
                Write(result);
            }
        }
    }
}
