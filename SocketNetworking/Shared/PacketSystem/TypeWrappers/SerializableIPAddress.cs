using System.Net;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.TypeWrappers
{
    [TypeWrapperAttribute(typeof(IPAddress))]
    public class SerializableIPAddress : TypeWrapper<IPAddress>
    {
        public override (IPAddress, int) Deserialize(byte[] data)
        {
            ByteReader byteReader = new ByteReader(data);
            byte[] ipBytes = byteReader.ReadByteArray();
            IPAddress ip = new IPAddress(ipBytes);
            return (ip, byteReader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            byte[] ipBytes = Value.GetAddressBytes();
            writer.WriteByteArray(ipBytes);
            return writer.Data;
        }
    }
}
