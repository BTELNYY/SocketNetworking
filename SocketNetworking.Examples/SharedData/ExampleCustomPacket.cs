﻿using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Server;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace SocketNetworking.Example.SharedData
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

    public struct ExampleStruct : IPacketSerializable
    {
        public int Value;

        public ulong Value2;

        public float Value3;

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Value = reader.ReadInt();
            Value2 = reader.ReadULong();
            Value3 = reader.ReadFloat();
            return reader.ReadBytes;
        }

        public int GetLength()
        {
            return sizeof(int) + sizeof(ulong) + sizeof(float);
            //or
            //return Serialize().Length;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(Value);
            writer.WriteULong(Value2);
            writer.WriteFloat(Value3);
            return writer.Data;
        }
    }
}
