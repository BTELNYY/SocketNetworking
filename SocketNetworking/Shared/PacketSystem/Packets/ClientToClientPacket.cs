﻿using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class ClientToClientPacket : TargetedPacket
    {
        public override PacketType Type => PacketType.ClientToClient;

        public byte[] Data { get; set; }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Data = reader.ReadByteArray();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteByteArray(Data);
            return writer;
        }
    }
}
