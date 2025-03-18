using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
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

        public ByteWriter() { }

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
                _workingSetData = _workingSetData.Concat(data).ToArray();
            }
        }

        public void WriteByteArray(byte[] data)
        {
            lock (_lock)
            {
                WriteInt(data.Length);
                _workingSetData = _workingSetData.Concat(data).ToArray();
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
                _workingSetData = _workingSetData.Concat(result).ToArray();
            }
        }

        public void WriteInt(int data)
        {
            lock (_lock)
            {
                int network = IPAddress.HostToNetworkOrder(data);
                byte[] result = BitConverter.GetBytes(network);
                _workingSetData = _workingSetData.Concat(result).ToArray();
            }
        }

        public void WriteShort(short data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data));
                _workingSetData = _workingSetData.Concat(result).ToArray();
            }
        }

        public void WriteULong(ulong data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)data));
                _workingSetData = _workingSetData.Concat(result).ToArray();
            }
        }

        public void WriteUInt(uint data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)data));
                _workingSetData = _workingSetData.Concat(result).ToArray();
            }
        }

        public void WriteUShort(ushort data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)data));
                _workingSetData = _workingSetData.Concat(result).ToArray();
            }
        }

        public void WriteFloat(float data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(data);
                _workingSetData = _workingSetData.Concat(result).ToArray();
            }
        }

        public void WriteDouble(double data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(data);
                _workingSetData = _workingSetData.Concat(result).ToArray();
            }
        }

        public void WriteString(string data)
        {
            lock (_lock)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                int curLength = _workingSetData.Length;
                WriteInt(bytes.Length);
                if (curLength + 4 > _workingSetData.Length)
                {
                    Log.GlobalWarning("WriteInt failed!");
                }
                _workingSetData = _workingSetData.Concat(bytes).ToArray();
            }
        }

        public void WriteBool(bool data)
        {
            lock (_lock)
            {
                byte[] result = BitConverter.GetBytes(data);
                _workingSetData = _workingSetData.Concat(result).ToArray();
            }
        }
    }
}
