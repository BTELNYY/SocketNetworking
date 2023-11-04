using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem
{
    public class PacketWriter
    {
        public int DataLength
        {
            get
            {
                return _workingSetData.Length;
            }
        }

        public byte[] Data
        {
            get
            {
                return _workingSetData;
            }
        }

        private byte[] _workingSetData = new byte[] { };

        public PacketWriter() { }

        public void Write<T>(IPacketSerializable serializable)
        {
            byte[] data = serializable.Serialize();
            _workingSetData.AppendAll(data);
        }

        public void Write(byte[] data)
        {
            _workingSetData.AppendAll(data);
        }

        public void WriteLong(long data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData.AppendAll(result);
        }

        public void WriteInt(int data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData.AppendAll(result);
        }

        public void WriteShort(short data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData.AppendAll(result);
        }

        public void WriteULong(ulong data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData.AppendAll(result);
        }

        public void WriteUInt(uint data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData.AppendAll(result);
        }

        public void WriteUShort(ushort data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData.AppendAll(result);
        }

        public void WriteFloat(float data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData.AppendAll(result);
        }

        public void WriteDouble(double data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData.AppendAll(result);
        }

        public void WriteString(string data)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(data);
            WriteInt(bytes.Length);
            _workingSetData.AppendAll(bytes);
        }

        public void WriteBool(bool data)
        {
            byte[] result = BitConverter.GetBytes(data);
            _workingSetData.AppendAll(result);
        }
    }
}
