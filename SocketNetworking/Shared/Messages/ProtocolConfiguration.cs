using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.Messages
{
    /// <summary>
    /// The <see cref="ProtocolConfiguration"/> class is used to communicate the version and protocol of the current connection.
    /// </summary>
    public class ProtocolConfiguration : IByteSerializable
    {
        /// <summary>
        /// The "internal name" of the protocol. For example: 'my-app', 'minecraft'. By default, this value is "default"
        /// </summary>
        public string Protocol
        {
            get
            {
                return _protocol;
            }
        }

        private string _protocol = "default";

        /// <summary>
        /// The version of this protocol. Any versioning style is accepted. By default, the version is 1.0.0
        /// </summary>
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
