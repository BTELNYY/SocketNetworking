using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SocketNetworking.Attributes;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Example.Basics.SharedData
{
    [PacketDefinition]
    public class SpamPacketTesting : CustomPacket
    {
        public byte ValueOne { get; set; } = 1;

        public int ValueTwo { get; set; } = 2;

        public float ValueThree { get; set; } = 3f;

        public bool ValueFour { get; set; } = true;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteByte(ValueOne);
            writer.WriteInt(ValueTwo);
            writer.WriteFloat(ValueThree);
            writer.WriteBool(ValueFour);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            ValueOne = reader.ReadByte();
            ValueTwo = reader.ReadInt();
            ValueThree = reader.ReadFloat();
            ValueFour = reader.ReadBool();
            return reader;
        }
    }
}
