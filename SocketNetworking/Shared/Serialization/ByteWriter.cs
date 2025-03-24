using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using SocketNetworking.Shared.Exceptions;
using SocketNetworking.Shared.PacketSystem;

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
        public int Length
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

        ~ByteWriter()
        {
            _workingSetData = null;
        }

        public void WriteObject<T>(T value)
        {
            lock (_lock)
            {
                SerializedData data = ByteConvert.Serialize(value);
                WritePacketSerialized<SerializedData>(data);
            }
        }

        public void WritePacketSerialized<T>(IPacketSerializable serializable)
        {
            lock (_lock)
            {
                byte[] data = serializable.Serialize().Data;
                Write(data);
            }
        }

        public void WriteWrapper(object any)
        {
            lock (_lock)
            {
                Type type = any.GetType();
                if (!NetworkManager.TypeToTypeWrapper.ContainsKey(type))
                {
                    throw new InvalidOperationException("No type wrapper for type: " + type.FullName);
                }
                object wrapper = Activator.CreateInstance(NetworkManager.TypeToTypeWrapper[type]);
                MethodInfo serializer = wrapper.GetType().GetMethod("Serialize");
                byte[] result = (byte[])serializer.Invoke(wrapper, new object[] { any });
                WriteByteArray(result);
            }
        }

        public void WriteWrapper<T, K>(T value) where T : TypeWrapper<K>
        {
            lock (_lock)
            {
                byte[] data = value.Serialize();
                WriteByteArray(data);
            }
        }

        public void Write(byte[] data)
        {
            lock (_lock)
            {
                _workingSetData = _workingSetData.FastConcat(data).ToArray();
            }
        }

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
                _workingSetData = _workingSetData.FastConcat(data).ToArray();
                if(_workingSetData.Length != expectedLength)
                {
                    throw new NetworkConversionException($"Wrote an invalid byte array! Expected: {expectedLength}, Actual: {_workingSetData.Length}");
                }
            }
        }

        public void WriteByte(byte data)
        {
            lock (_lock)
            {
                _workingSetData = _workingSetData.Append(data).ToArray();
            }
        }

        public void WriteSByte(sbyte data)
        {
            lock (_lock)
            {
                byte written = Convert.ToByte(data);
                _workingSetData = _workingSetData.Append(written).ToArray();
            }
        }

        public void WriteLong(long data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data));
                _workingSetData = _workingSetData.FastConcat(result).ToArray();
            }
        }

        public void WriteInt(int data)
        {
            lock (_lock)
            {
                int network = IPAddress.HostToNetworkOrder(data);
                byte[] result = BitConverter.GetBytes(network);
                _workingSetData = _workingSetData.FastConcat(result).ToArray();
            }
        }

        public void WriteShort(short data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data));
                _workingSetData = _workingSetData.FastConcat(result).ToArray();
            }
        }

        public void WriteULong(ulong data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)data));
                _workingSetData = _workingSetData.FastConcat(result).ToArray();
            }
        }

        public void WriteUInt(uint data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)data));
                _workingSetData = _workingSetData.FastConcat(result).ToArray();
            }
        }

        public void WriteUShort(ushort data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)data));
                _workingSetData = _workingSetData.FastConcat(result).ToArray();
            }
        }

        public void WriteFloat(float data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(data);
                _workingSetData = _workingSetData.FastConcat(result).ToArray();
            }
        }

        public void WriteDouble(double data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(data);
                _workingSetData = _workingSetData.FastConcat(result).ToArray();
            }
        }

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

        public void WriteBool(bool data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(data);
                _workingSetData = _workingSetData.FastConcat(result).ToArray();
            }
        }
    }
}
