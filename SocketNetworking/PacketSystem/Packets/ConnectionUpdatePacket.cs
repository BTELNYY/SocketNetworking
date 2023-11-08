using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;

namespace SocketNetworking.PacketSystem.Packets
{
    [PacketDefinition]
    public sealed class ConnectionUpdatePacket : Packet
    {
        public sealed override PacketType Type => PacketType.ConnectionStateUpdate;

        public ConnectionState State  = ConnectionState.Disconnected;

        public string Reason  = "No comment";

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            State = (ConnectionState)reader.ReadInt();
            Reason = reader.ReadString();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt((int)State);
            writer.WriteString(Reason);
            return writer;
        }
    }
}
