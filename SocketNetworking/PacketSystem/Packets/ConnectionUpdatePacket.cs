using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.Shared;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class ConnectionUpdatePacket : Packet
    {
        public sealed override PacketType Type => PacketType.ConnectionStateUpdate;

        public ConnectionState State  = ConnectionState.Disconnected;

        public string Reason  = "No comment";

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Reason = reader.ReadString();
            State = (ConnectionState)reader.ReadInt();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteString(Reason);
            writer.WriteInt((int)State);
            return writer;
        }
    }
}
