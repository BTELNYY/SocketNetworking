using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.Misc;
using SocketNetworking.Shared;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class ClientDataPacket : Packet
    {
        public sealed override PacketType Type => PacketType.ClientData;

        public ProtocolConfiguration Configuration { get; set; } = new ProtocolConfiguration();
        
        public string PasswordHash { get; private set; } = "lol";

        public ClientDataPacket(string password) 
        {
            PasswordHash = password.GetStringHash();
        }

        public ClientDataPacket()
        {

        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            PasswordHash = reader.ReadString();
            Configuration = reader.ReadPacketSerialized<ProtocolConfiguration>();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteString(PasswordHash);
            writer.WritePacketSerialized<ProtocolConfiguration>(Configuration);
            return writer;
        }
    }
}
