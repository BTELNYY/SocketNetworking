using SocketNetworking.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.TypeWrappers
{
    [TypeWrapperAttribute(typeof(IPEndPoint))]
    public class SerializableIPEndpoinrt : TypeWrapper<IPEndPoint>
    {
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
