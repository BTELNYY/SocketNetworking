using SocketNetworking.PacketSystem;
using SocketNetworking.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared.Messages
{
    public class ProtocolConfiguration : IPacketSerializable
    {
        public string Protocol
        {
            get
            {
                return _protocol;
            }
        }

        private string _protocol = "default";

        public string Version
        {
            get
            {
                return _version;
            }
        }

        private string _version = "1.0.0";

        public ProtocolConfiguration(string protocol, string version)
        {
            _protocol = protocol;
            _version = version;
        }

        public ProtocolConfiguration()
        {

        }

        public override string ToString()
        {
            return $"Protocol: {Protocol}, Version: {Version}";
        }

        public int GetLength()
        {
            int count = Serialize().Data.Length;
            return count;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(_version);
            writer.WriteString(_protocol);
            return writer;
        }

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            _version = reader.ReadString();
            _protocol = reader.ReadString();
            return reader;
        }
    }
}
