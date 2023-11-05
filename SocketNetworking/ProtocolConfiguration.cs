using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking
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
            int count = sizeof(int) * 2;
            count += _version.SerializedSize();
            count += _protocol.SerializedSize();
            return count;
        }

        public byte[] Serialize()
        {
            PacketWriter writer = new PacketWriter();
            writer.WriteString(_protocol);
            writer.WriteString(_version);
            return writer.Data;
        }

        public int Deserialize(byte[] data)
        {
            PacketReader reader = new PacketReader(data);
            _version = reader.ReadString();
            _protocol = reader.ReadString();
            return reader.ReadBytes;
        }
    }
}
