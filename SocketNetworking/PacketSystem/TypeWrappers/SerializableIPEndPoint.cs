using System;
using System.Net;
using SocketNetworking.Attributes;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.PacketSystem.TypeWrappers
{
    [TypeWrapperAttribute(typeof(IPEndPoint))]
    public class SerializableIPEndPoint : TypeWrapper<IPEndPoint>
    {
        public SerializableIPEndPoint()
        {
        }

        public SerializableIPEndPoint(IPEndPoint value) : base(value)
        {
        }


        public override (IPEndPoint, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            SerializableIPAddress serializableIP = new SerializableIPAddress();
            ValueTuple<IPAddress, int> desil = serializableIP.Deserialize(data);
            reader.Remove(desil.Item2);
            int port = reader.ReadUShort();
            IPEndPoint endPoint = new IPEndPoint(desil.Item1, port);
            return (endPoint, reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            if(Value == null)
            {
                Log.GlobalWarning("Value is null! Cannot serialize, default address used.");
                Value = new IPEndPoint(0, 0);
            }
            IPAddress target = Value.Address;
            int targetPort = Value.Port;
            SerializableIPAddress serializableIP = new SerializableIPAddress();
            serializableIP.Value = target;
            byte[] seralizedIp = serializableIP.Serialize();
            writer.Write(seralizedIp);
            writer.WriteUShort((ushort)targetPort);
            return writer.Data;
        }
    }
}
