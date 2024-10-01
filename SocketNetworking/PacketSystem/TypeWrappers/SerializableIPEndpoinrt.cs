using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.TypeWrappers
{
    public class SerializableIPEndpoinrt : TypeWrapper<IPEndPoint>
    {
        public override (IPEndPoint, int) Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            IPAddress result = reader.ReadWrapper<SerializableIPAddress, IPAddress>();
            int port = reader.ReadUShort();
            IPEndPoint endPoint = new IPEndPoint(result, port);
            return (endPoint, reader.ReadBytes);
        }

        public override byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
