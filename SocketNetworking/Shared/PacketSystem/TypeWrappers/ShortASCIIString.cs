using System.Linq;
using System.Text;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.TypeWrappers
{
    /// <summary>
    /// Represents an <see cref="Encoding.ASCII"/> string that is alos shorter than or equal to, 255 bytes. Useful for simple messages that don't use many bytes.
    /// </summary>
    public sealed class ShortASCIIString : IByteSerializable
    {
        public string Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = new string(value.Take(byte.MaxValue).ToArray());
            }
        }

        private string _data;

        public byte Size { get; private set; }

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Size = reader.ReadByte();
            byte[] strContent = reader.Read(Size);
            Data = Encoding.ASCII.GetString(strContent);
            return reader;
        }

        public int GetLength()
        {
            return (int)Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            byte[] strContent = Encoding.ASCII.GetBytes(Data);
            writer.WriteByte((byte)strContent.Length);
            writer.Write(strContent);
            return writer;
        }
    }
}
