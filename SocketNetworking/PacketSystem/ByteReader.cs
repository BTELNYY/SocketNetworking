using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem
{
    /// <summary>
    /// Provides Packet reading capablities by reading elements of the byte array.
    /// </summary>
    public class ByteReader
    {
        /// <summary>
        /// Represents the original untouched data fed into the class. This data is not changed by any method.
        /// </summary>
        public byte[] RawData { get; private set; }

        /// <summary>
        /// Amount of bytes that are unread
        /// </summary>
        public int DataLength
        {
            get
            {
                return _workingSetData.Length;
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
                return RawData.Length - _workingSetData.Length;
            }
        }


        public ByteReader(byte[] data)
        {
            RawData = data;
            _workingSetData = RawData;
        }

        public void Remove(int length)
        {
            _workingSetData.RemoveFromStart(length);
        }

        public byte[] Read(int length)
        {
            byte[] data = _workingSetData.Take(length).ToArray();
            _workingSetData = _workingSetData.RemoveFromStart(length);
            return data;
        }

        public T Read<T>() where T : IPacketSerializable
        {
            IPacketSerializable serializable = (IPacketSerializable)Activator.CreateInstance(typeof(T));
            int bytesUsed = serializable.Deserialize(_workingSetData);
            _workingSetData = _workingSetData.RemoveFromStart(bytesUsed);
            return (T)serializable;
        }

        public byte ReadByte()
        {
            byte result = _workingSetData[0];
            _workingSetData = _workingSetData.RemoveFromStart(1);
            return result;
        }

        public sbyte ReadSByte()
        {
            sbyte result = Convert.ToSByte(ReadByte());
            return result;
        }

        public ulong ReadULong()
        {
            int sizeToRemove = sizeof(ulong);
            ulong result = BitConverter.ToUInt64(_workingSetData, 0);
            _workingSetData = _workingSetData.RemoveFromStart(sizeToRemove);
            return result;
        }

        public uint ReadUInt()
        {
            int sizeToRemove = sizeof(uint);
            uint result = BitConverter.ToUInt32(_workingSetData, 0);
            _workingSetData = _workingSetData.RemoveFromStart(sizeToRemove);
            return result;
        }

        public ushort ReadUShort()
        {
            int sizeToRemove = sizeof(ushort);
            ushort result = BitConverter.ToUInt16(_workingSetData, 0);
            _workingSetData = _workingSetData.RemoveFromStart(sizeToRemove);
            return result;
        }

        public long ReadLong()
        {
            int sizeToRemove = sizeof(long);
            long result = BitConverter.ToInt64(_workingSetData, 0);
            _workingSetData = _workingSetData.RemoveFromStart(sizeToRemove);
            return result;
        }

        public int ReadInt()
        {
            int sizeToRemove = sizeof(int);
            int result = BitConverter.ToInt32(_workingSetData, 0);
            _workingSetData = _workingSetData.RemoveFromStart(sizeToRemove);
            return result;
        }

        public short ReadShort()
        {
            int sizeToRemove = sizeof(short);
            short result = BitConverter.ToInt16(_workingSetData, 0);
            _workingSetData = _workingSetData.RemoveFromStart(sizeToRemove);
            return result;
        }

        public float ReadFloat()
        {
            int sizeToRemove = sizeof(float);
            float result = BitConverter.ToSingle(_workingSetData, 0);
            _workingSetData = _workingSetData.RemoveFromStart(sizeToRemove);
            return result;
        }

        public double ReadDouble()
        {
            int sizeToRemove = sizeof(double);
            double result = BitConverter.ToDouble(_workingSetData, 0);
            _workingSetData = _workingSetData.RemoveFromStart(sizeToRemove);
            return result;
        }

        public string ReadString()
        {
            int lenghtOfString = ReadInt();
            string result = Encoding.UTF8.GetString(_workingSetData, 0, lenghtOfString);
            _workingSetData = _workingSetData.RemoveFromStart(lenghtOfString);
            return result;
        }

        public bool ReadBool()
        {
            int sizeToRemove = sizeof(bool);
            bool result = BitConverter.ToBoolean(_workingSetData, 0);
            _workingSetData = _workingSetData.RemoveFromStart(sizeToRemove);
            return result;
        }
    }
}
