using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Example.Basics.SharedData
{
    [PacketDefinition]
    public class ExampleCustomPacket : CustomPacket
    {
        public override int CustomPacketID => NetworkManager.GetAutoPacketID(this);

        public string Data { get; set; } = "DataTest!";

        public ExampleStruct Struct { get; set; } = new ExampleStruct()
        {
            Value = 3,
            Value2 = 0938484048,
            Value3 = 0.4f,
        };

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Data = reader.ReadString();
            Struct = reader.ReadPacketSerialized<ExampleStruct>();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteString(Data);
            writer.WritePacketSerialized<ExampleStruct>(Struct);
            return writer;
        }
    }

    public struct ExampleStruct : IByteSerializable
    {
        public int Value;

        public ulong Value2;

        public float Value3;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Value = reader.ReadInt();
            Value2 = reader.ReadULong();
            Value3 = reader.ReadFloat();
            return reader;
        }

        public int GetLength()
        {
            return sizeof(int) + sizeof(ulong) + sizeof(float);
            //or
            //return Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(Value);
            writer.WriteULong(Value2);
            writer.WriteFloat(Value3);
            return writer;
        }

        public override string ToString()
        {
            return $"{Value}, {Value2}, {Value3}";
        }
    }
}
