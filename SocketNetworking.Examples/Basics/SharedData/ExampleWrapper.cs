using SocketNetworking.PacketSystem;
using SocketNetworking.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

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
