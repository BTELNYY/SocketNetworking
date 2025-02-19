﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;

namespace SocketNetworking.PacketSystem
{
    /// <summary>
    /// Provides buffer manipulation methods.
    /// </summary>
    public class ByteWriter
    {
        /// <summary>
        /// The current length of the buffer.
        /// </summary>
        public int DataLength
        {
            get
            {
                return _workingSetData.Length;
            }
        }

        /// <summary>
        /// The written data buffer.
        /// </summary>
        public byte[] Data
        {
            get
            {
                return _workingSetData;
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

        public void WritePacketSerialized<T>(IPacketSerializable serializable)
        {
            byte[] data = serializable.Serialize();
            Write(data);
        }

        public void WriteWrapper(object any)
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

        public void WriteWrapper<T, K>(T value) where T : TypeWrapper<K>
        {
            byte[] data = value.Serialize();
            WriteByteArray(data);
        }

        public void Write(byte[] data)
        {
            _workingSetData = _workingSetData.Concat(data).ToArray();
        }

        public void WriteByteArray(byte[] data)
        {
            WriteInt(data.Length);
            _workingSetData = _workingSetData.Concat(data).ToArray();
        }

        public void WriteByte(byte data)
        {
            _workingSetData = _workingSetData.Append(data).ToArray();
        }

        public void WriteSByte(sbyte data)
        {
            byte written = Convert.ToByte(data);
            _workingSetData = _workingSetData.Append(written).ToArray();
        }

        public void WriteLong(long data)
        {
            byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data));
            _workingSetData = _workingSetData.Concat(result).ToArray();
        }

        public void WriteInt(int data)
        {
            int network = IPAddress.HostToNetworkOrder(data);
            byte[] result = BitConverter.GetBytes(network);
            _workingSetData = _workingSetData.Concat(result).ToArray();
        }

        public void WriteShort(short data)
        {
            byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data));
            _workingSetData = _workingSetData.Concat(result).ToArray();
        }

        public void WriteULong(ulong data)
        {
            byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)data));
            _workingSetData = _workingSetData.Concat(result).ToArray();
        }

        public void WriteUInt(uint data)
        {
            byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)data));
            _workingSetData = _workingSetData.Concat(result).ToArray();
        }

        public void WriteUShort(ushort data)
        {
            byte[] result = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)data));
            _workingSetData = _workingSetData.Concat(result).ToArray();
        }

        public void WriteFloat(float data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData = _workingSetData.Concat(result).ToArray();
        }

        public void WriteDouble(double data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData = _workingSetData.Concat(result).ToArray();
        }

        public void WriteString(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            int curLength = _workingSetData.Length;
            WriteInt(bytes.Length);
            if(curLength + 4 > _workingSetData.Length)
            {
                Log.GlobalWarning("WriteInt failed!");
            }
            _workingSetData = _workingSetData.Concat(bytes).ToArray();
        }

        public void WriteBool(bool data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData = _workingSetData.Concat(result).ToArray();
        }
    }
}
