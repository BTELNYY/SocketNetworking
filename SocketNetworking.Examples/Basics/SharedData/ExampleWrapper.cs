using System;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Example.Basics.SharedData
{
    public class ExampleWrapper : TypeWrapper<ValueTuple<int, int>>
    {
        public override ((int, int), int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            ValueTuple<int, int> result = (reader.ReadInt(), reader.ReadInt());
            return (result, reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(Value.Item1);
            writer.WriteInt(Value.Item2);
            return writer.Data;
        }
    }
}
