using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.Streams
{
    public class NetworkFileStream : SyncedStream
    {
        public NetworkFileStream(NetworkClient client, ushort id) : base(client, id, 0)
        {

        }

        public void SendFile(string path)
        {
            string extension = Path.GetFileName(path);
            FileStream stream = File.OpenRead(path);
        }

    }

    public struct FileInfo : IPacketSerializable
    {
        public ushort FileID;

        public ulong Size;

        public string Name;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            FileID = reader.ReadUShort();
            Size = reader.ReadULong();
            Name = reader.ReadString();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().DataLength;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteUShort(FileID);
            writer.WriteULong(Size);
            writer.WriteString(Name);
            return writer;
        }
    }
}
