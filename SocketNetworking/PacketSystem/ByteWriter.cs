using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem
{
    public class ByteWriter
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

        public ByteWriter() { }

        ~ByteWriter()
        {
            _workingSetData = null;
        }

        public void Write<T>(IPacketSerializable serializable)
        {
            byte[] data = serializable.Serialize();
            Write(data);
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
