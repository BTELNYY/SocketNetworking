using System.Net;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.TypeWrappers
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
            IPAddress desil = reader.ReadWrapper<SerializableIPAddress, IPAddress>();
            int port = reader.ReadUShort();
            IPEndPoint endPoint = new IPEndPoint(desil, port);
            return (endPoint, reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            if (Value == null)
            {
                Log.GlobalWarning("Value is null! Cannot serialize, default address used.");
                Value = new IPEndPoint(0, 0);
            }
            IPAddress target = Value.Address;
            int targetPort = Value.Port;
            SerializableIPAddress serializableIP = new SerializableIPAddress();
            serializableIP.Value = target;
            writer.WriteWrapper<SerializableIPAddress, IPAddress>(serializableIP);
            writer.WriteUShort((ushort)targetPort);
            return writer.Data;
        }
    }
}
