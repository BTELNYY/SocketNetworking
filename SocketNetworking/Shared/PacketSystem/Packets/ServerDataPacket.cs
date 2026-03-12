using System.Collections.Generic;
using SocketNetworking.Shared.Messages;
using SocketNetworking.Shared.PacketSystem.TypeWrappers;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class ServerDataPacket : Packet
    {
        public sealed override PacketType Type => PacketType.ServerData;

        public int YourClientID { get; set; } = 0;

        public ProtocolConfiguration Configuration { get; set; } = new ProtocolConfiguration();

        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

        public bool UpgradeToSSL
        {
            get
            {
                return Headers.ContainsKey(HeaderConstants.HEADER_SSL_SUPPORTED) ? Headers[HeaderConstants.HEADER_SSL_SUPPORTED] == HeaderConstants.HEADER_TRUE : false;
            }
            set
            {
                if (Headers.ContainsKey(HeaderConstants.HEADER_SSL_SUPPORTED))
                {
                    Headers[HeaderConstants.HEADER_SSL_SUPPORTED] = value ? HeaderConstants.HEADER_TRUE : HeaderConstants.HEADER_FALSE;
                }
                else
                {
                    Headers.Add(HeaderConstants.HEADER_SSL_SUPPORTED, value ? HeaderConstants.HEADER_TRUE : HeaderConstants.HEADER_FALSE);
                }
            }
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(YourClientID);
            writer.WritePacketSerialized<ProtocolConfiguration>(Configuration);
            writer.WritePacketSerialized<SerializableDictionary<string, string>>(new SerializableDictionary<string, string>(Headers));
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            YourClientID = reader.ReadInt();
            Configuration = reader.ReadPacketSerialized<ProtocolConfiguration>();
            Headers = reader.ReadPacketSerialized<SerializableDictionary<string, string>>().ContainedDict;
            return reader;
        }
    }
}
